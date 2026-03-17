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
        private readonly IReadOnlyList<TcpListener> _listeners;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _responseSent;

        public LoopbackHttpCallbackSession(Uri callbackUri)
        {
            _callbackUri = callbackUri;
            _listeners = CreateListeners(callbackUri.Port);
        }

        public Uri CallbackUri => _callbackUri;

        public async Task<LocalHttpCallbackRequest> WaitForCallbackAsync(CancellationToken cancellationToken = default)
        {
            if (_client is not null)
            {
                throw new InvalidOperationException("This callback session has already accepted a request.");
            }

            _client = await AcceptClientAsync(cancellationToken).ConfigureAwait(false);
            _stream = _client.GetStream();

            using var reader = new StreamReader(_stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

            string requestLine = await ReadRequiredLineAsync(reader).ConfigureAwait(false);
            string[] parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                throw new InvalidOperationException("Received an invalid HTTP request line.");
            }

            while (true)
            {
                string? headerLine = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(headerLine))
                {
                    break;
                }
            }

            Uri requestUri = BuildRequestUri(parts[1]);
            return new LocalHttpCallbackRequest(
                requestUri,
                parts[0],
                ParseQueryParameters(requestUri));
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
            try
            {
                _stream?.Dispose();
            }
            catch
            {
            }

            try
            {
                _client?.Dispose();
            }
            catch
            {
            }

            foreach (TcpListener listener in _listeners)
            {
                try
                {
                    listener.Stop();
                }
                catch
                {
                }
            }

            return ValueTask.CompletedTask;
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

        private async Task<TcpClient> AcceptClientAsync(CancellationToken cancellationToken)
        {
            List<Task<TcpClient>> acceptTasks = [];
            foreach (TcpListener listener in _listeners)
            {
                acceptTasks.Add(listener.AcceptTcpClientAsync(cancellationToken).AsTask());
            }

            Task<TcpClient> completed = await Task.WhenAny(acceptTasks).ConfigureAwait(false);

            foreach (TcpListener listener in _listeners)
            {
                try
                {
                    listener.Stop();
                }
                catch
                {
                }
            }

            return await completed.ConfigureAwait(false);
        }

        private Uri BuildRequestUri(string target)
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out Uri? absolute))
            {
                return absolute;
            }

            if (!target.StartsWith("/", StringComparison.Ordinal))
            {
                target = "/" + target;
            }

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
