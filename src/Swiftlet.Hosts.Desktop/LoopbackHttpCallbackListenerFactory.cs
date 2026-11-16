using System.Net;
using System.Net.Sockets;
using System.Text;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Desktop;

public sealed class LoopbackHttpCallbackListenerFactory : ILocalHttpCallbackListenerFactory
{
    public ValueTask<ILocalHttpCallbackSession> StartAsync(Uri callbackUri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callbackUri);

        if (!string.Equals(callbackUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Only http:// loopback callbacks are currently supported.");
        }

        if (!callbackUri.IsLoopback)
        {
            throw new NotSupportedException("Only loopback callback URIs are supported.");
        }

        if (callbackUri.Port < 1 || callbackUri.Port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(callbackUri), "Callback URI must include a valid port.");
        }

        return ValueTask.FromResult<ILocalHttpCallbackSession>(new LoopbackHttpCallbackSession(callbackUri));
    }

    private sealed class LoopbackHttpCallbackSession : ILocalHttpCallbackSession
    {
        private readonly Uri _callbackUri;
        private readonly object _sync = new();
        private readonly CancellationTokenSource _lifetime = new();
        private Task<LocalHttpCallbackRequest>? _pendingRead;
        private Task? _disposeTask;
        private bool _disposed;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _responseSent;

        public LoopbackHttpCallbackSession(Uri callbackUri)
        {
            _callbackUri = callbackUri;
        }

        public Uri CallbackUri => _callbackUri;

        public Task<LocalHttpCallbackRequest> WaitForCallbackAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(LoopbackHttpCallbackSession));
                if (_client is not null || _pendingRead is { IsCompleted: false })
                    throw new InvalidOperationException("This callback session is already receiving a request.");
                return _pendingRead = ReadCallbackAsync(cancellationToken);
            }
        }

        private sealed record ReceivedCallback(TcpClient Client, NetworkStream Stream, LocalHttpCallbackRequest Request);

        private async Task<LocalHttpCallbackRequest> ReadCallbackAsync(CancellationToken cancellationToken)
        {
            using var receive = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            IReadOnlyList<TcpListener> listeners = CreateListeners(_callbackUri.Port);
            var acceptedClients = new Queue<TcpClient>();
            var readers = new List<Task>();
            var winner = new TaskCompletionSource<ReceivedCallback>(TaskCreationOptions.RunContinuationsAsynchronously);
            bool published = false;

            async Task ReadClientAsync(TcpClient client)
            {
                bool keepClient = false;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(receive.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                using var cancellation = timeout.Token.Register(client.Dispose);
                try
                {
                    NetworkStream stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    var request = await ReadRequestAsync(reader).ConfigureAwait(false);
                    if (!string.Equals(request.RequestUri.AbsolutePath, _callbackUri.AbsolutePath, StringComparison.Ordinal))
                    {
                        byte[] reply = Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(reply, timeout.Token).ConfigureAwait(false);
                        return;
                    }
                    timeout.CancelAfter(Timeout.InfiniteTimeSpan);
                    timeout.Token.ThrowIfCancellationRequested();
                    // Once selected, the response socket belongs to the session, not to
                    // the receive-loop cancellation used to shut down losing readers.
                    cancellation.Dispose();
                    keepClient = winner.TrySetResult(new ReceivedCallback(client, stream, request));
                }
                catch (Exception error) when (error is IOException or SocketException or InvalidOperationException or FormatException or ObjectDisposedException or OperationCanceledException)
                { /* Empty, malformed and timed-out connections are not callbacks. */ }
                catch (Exception error) { winner.TrySetException(error); }
                finally { if (!keepClient) client.Dispose(); }
            }

            async Task AcceptAsync()
            {
                while (true)
                {
                    receive.Token.ThrowIfCancellationRequested();
                    TcpClient client = await AcceptClientAsync(listeners, acceptedClients, receive.Token).ConfigureAwait(false);
                    // An idle browser preconnect must not delay the real callback.
                    readers.Add(ReadClientAsync(client));
                }
            }

            Task accepting = AcceptAsync();
            try
            {
                Task completed = await Task.WhenAny(winner.Task, accepting).ConfigureAwait(false);
                if (ReferenceEquals(completed, accepting)) await accepting.ConfigureAwait(false);
                ReceivedCallback result = await winner.Task.ConfigureAwait(false);
                lock (_sync)
                {
                    if (_disposed || receive.IsCancellationRequested)
                        throw new OperationCanceledException(receive.Token);
                    _client = result.Client;
                    _stream = result.Stream;
                    _responseSent = false;
                    published = true;
                }
                return result.Request;
            }
            finally
            {
                receive.Cancel();
                foreach (TcpListener listener in listeners) listener.Stop();
                try { await accepting.ConfigureAwait(false); }
                catch (Exception) { /* Observe the canceled accept before enumerating its readers. */ }
                await Task.WhenAll(readers).ConfigureAwait(false);
                while (acceptedClients.TryDequeue(out var queued)) queued.Dispose();
                if (!published && winner.Task.IsCompletedSuccessfully) winner.Task.Result.Client.Dispose();
            }
        }

        private async Task<LocalHttpCallbackRequest> ReadRequestAsync(StreamReader reader)
        {
            string requestLine = await ReadRequiredLineAsync(reader).ConfigureAwait(false);
            string[] parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 || !parts[0].All(char.IsAsciiLetter) ||
                parts[2] is not ("HTTP/1.0" or "HTTP/1.1"))
                throw new FormatException("Received an invalid HTTP request line.");
            Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null) throw new IOException("Incomplete HTTP headers.");
                if (line.Length == 0) break;
                if (line.IndexOf(':') <= 0) throw new FormatException("Invalid HTTP header.");
                TryAddHeader(line, headers);
            }
            string body = await ReadRequestBodyAsync(reader, headers).ConfigureAwait(false);
            Uri uri = BuildRequestUri(parts[1]);
            return new LocalHttpCallbackRequest(uri, parts[0], ParseCallbackParameters(uri, parts[0], headers, body), headers, body);
        }

        public async Task SendResponseAsync(LocalHttpCallbackResponse response, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(response);

            if (_stream is null)
            {
                throw new InvalidOperationException("No callback request has been received.");
            }

            if (_responseSent)
            {
                throw new InvalidOperationException("A callback response has already been sent.");
            }

            byte[] body = response.GetBodyBytes();
            string header =
                $"HTTP/1.1 {response.StatusCode} {GetReasonPhrase(response.StatusCode)}\r\n" +
                $"Content-Type: {response.ContentType}\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                "Connection: close\r\n\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(header);

            await _stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
            await _stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            _responseSent = true;
            _stream.Dispose();
            _stream = null;
            _client?.Dispose();
            _client = null;
        }

        public ValueTask DisposeAsync()
        {
            lock (_sync)
                return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }

        private async Task DisposeCoreAsync()
        {
            _disposed = true;
            _lifetime.Cancel();
            _stream?.Dispose();
            _client?.Dispose();
            // ReadCallbackAsync owns both IPv4/IPv6 listeners and stops them in finally.
            // Join that operation before returning: cancellation alone merely schedules
            // its continuation and does not prove the callback port has been released.
            if (_pendingRead is { } pending)
            {
                try { await pending.ConfigureAwait(false); }
                catch (Exception) { /* The waiter retains the original failure; disposal observes it. */ }
            }
            _lifetime.Dispose();
        }

        private static IReadOnlyList<TcpListener> CreateListeners(int port)
        {
            List<TcpListener> listeners = [];

            listeners.Add(StartListener(IPAddress.Loopback, port));

            try
            {
                listeners.Add(StartListener(IPAddress.IPv6Loopback, port));
            }
            catch (SocketException)
            {
            }
            catch (NotSupportedException)
            {
            }

            return listeners;
        }

        private static TcpListener StartListener(IPAddress address, int port)
        {
            var listener = new TcpListener(address, port);
            listener.Start();
            return listener;
        }

        private static async Task<TcpClient> AcceptClientAsync(IReadOnlyList<TcpListener> listeners, Queue<TcpClient> acceptedClients, CancellationToken cancellationToken)
        {
            if (acceptedClients.TryDequeue(out var queued)) return queued;
            using var acceptCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var accepts = listeners.Select(listener => listener.AcceptTcpClientAsync(acceptCancellation.Token).AsTask()).ToArray();
            TcpClient? accepted = null;
            try
            {
                Task<TcpClient> completed = await Task.WhenAny(accepts).ConfigureAwait(false);
                return accepted = await completed.ConfigureAwait(false);
            }
            finally
            {
                // Cancel the losing IPv4/IPv6 accept without closing the listening sockets.
                acceptCancellation.Cancel();
                foreach (Task<TcpClient> task in accepts)
                {
                    try
                    {
                        TcpClient other = await task.ConfigureAwait(false);
                        if (!ReferenceEquals(other, accepted)) acceptedClients.Enqueue(other);
                    }
                    catch (Exception) { /* Observe canceled accepts while preserving the winner's result. */ }
                }
            }
        }

        private Uri BuildRequestUri(string target)
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out Uri? absolute))
            {
                if (string.Equals(absolute.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(absolute.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    return absolute;
                }

                // Some macOS browser/provider combinations send a malformed absolute
                // target like file:///callback/%3Fcode=... to the loopback listener.
                // Reinterpret that file path against the expected callback base so the
                // encoded query marker becomes a real query string again.
                if (string.Equals(absolute.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    string fileTarget = Uri.UnescapeDataString(absolute.AbsolutePath);
                    if (!fileTarget.StartsWith("/", StringComparison.Ordinal))
                    {
                        fileTarget = "/" + fileTarget;
                    }

                    return new Uri(_callbackUri, fileTarget);
                }
            }

            if (!target.StartsWith("/", StringComparison.Ordinal))
            {
                target = "/" + target;
            }

            target = Uri.UnescapeDataString(target);
            return new Uri(_callbackUri, target);
        }

        private static async Task<string> ReadRequiredLineAsync(StreamReader reader)
        {
            string? line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new InvalidOperationException("Received an empty HTTP request.");
            }

            return line;
        }

        private static IReadOnlyDictionary<string, string?> ParseQueryParameters(Uri requestUri)
        {
            Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);

            string query = requestUri.Query;
            if (string.IsNullOrEmpty(query))
            {
                return values;
            }

            ReadOnlySpan<char> span = query.AsSpan();
            if (span[0] == '?')
            {
                span = span[1..];
            }

            foreach (string segment in span.ToString().Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = segment.Split('=', 2);
                string key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
                string? value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
                values[key] = value;
            }

            return values;
        }

        private static IReadOnlyDictionary<string, string?> ParseCallbackParameters(
            Uri requestUri,
            string httpMethod,
            IReadOnlyDictionary<string, string> headers,
            string requestBody)
        {
            Dictionary<string, string?> values = new(ParseQueryParameters(requestUri), StringComparer.OrdinalIgnoreCase);

            if (string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(requestBody) &&
                headers.TryGetValue("Content-Type", out string? contentType) &&
                IsFormUrlEncoded(contentType))
            {
                foreach ((string key, string? value) in ParseFormParameters(requestBody))
                {
                    values[key] = value;
                }
            }

            return values;
        }

        private static IEnumerable<KeyValuePair<string, string?>> ParseFormParameters(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                yield break;
            }

            foreach (string segment in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = segment.Split('=', 2);
                string key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
                string? value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
                yield return new KeyValuePair<string, string?>(key, value);
            }
        }

        private static bool IsFormUrlEncoded(string contentType)
        {
            return contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryAddHeader(string headerLine, IDictionary<string, string> headers)
        {
            int separatorIndex = headerLine.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return;
            }

            string name = headerLine[..separatorIndex].Trim();
            string value = headerLine[(separatorIndex + 1)..].Trim();
            if (name.Length > 0)
            {
                headers[name] = value;
            }
        }

        private static async Task<string> ReadRequestBodyAsync(
            StreamReader reader,
            IReadOnlyDictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Content-Length", out string? contentLengthText) ||
                !int.TryParse(contentLengthText, out int contentLength) ||
                contentLength <= 0)
            {
                return string.Empty;
            }

            char[] buffer = new char[contentLength];
            int totalRead = 0;
            while (totalRead < contentLength)
            {
                int read = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            return totalRead == 0 ? string.Empty : new string(buffer, 0, totalRead);
        }

        private static string GetReasonPhrase(int statusCode)
        {
            return statusCode switch
            {
                200 => "OK",
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "OK",
            };
        }
    }
}
