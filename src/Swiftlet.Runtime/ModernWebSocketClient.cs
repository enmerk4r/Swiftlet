using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Swiftlet.Runtime;

public sealed class ModernWebSocketClient : IAsyncDisposable
{
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly object _sync = new();
    private ClientWebSocket? _client;
    private ModernWebSocketConnection? _connection;
    private CancellationTokenSource? _receiveLoopCancellation;
    private string? _lastError;

    public event EventHandler? StateChanged;

    public ModernWebSocketConnection? Connection => _connection;

    public string? LastError => _lastError;

    public bool IsConnected => _connection?.IsOpen == true;

    /// <summary>
    /// Upper bound for opening a connection (TCP connect plus the WebSocket handshake). Without it, an
    /// endpoint that accepts TCP but never answers the upgrade keeps <see cref="ConnectAsync"/> waiting
    /// forever; GH1 connects synchronously on the UI thread (also when it reconnects after the server
    /// closed), so that froze Rhino. A healthy local or internet handshake takes well under a second;
    /// 10 s leaves room for slow networks and TLS while keeping a stall short. It matches the bridge
    /// start-up limit used by the WebSocket and HTTP servers.
    /// </summary>
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    public async Task ConnectAsync(string url, CancellationToken cancellationToken = default)
    {
        // The caller replaces the connection and reads the result itself; closing the old one is not a
        // separate notification (only the outcome of the connect is).
        await DisconnectCoreAsync(notify: false).ConfigureAwait(false);

        var client = new ClientWebSocket();
        using (var connectLimit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            connectLimit.CancelAfter(ConnectTimeout);
            try
            {
                await client.ConnectAsync(new Uri(url), connectLimit.Token).ConfigureAwait(false);
            }
            catch (Exception error) when (connectLimit.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Our own limit, not the caller's cancellation: report it like any other connect failure.
                client.Dispose();
                _lastError = $"Connection timed out after {ConnectTimeout.TotalSeconds:0} s (no WebSocket handshake from {url})";
                throw new TimeoutException(_lastError, error);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                // Failed connects show as "Error: ..." in the Status output, as errors on a live connection do.
                client.Dispose();
                _lastError = error.Message;
                throw;
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connection = new ModernWebSocketConnection(client, false, url, "client");

        lock (_sync)
        {
            _client = client;
            _connection = connection;
            _receiveLoopCancellation = linkedCancellation;
            _lastError = null;
            _ = Task.Run(() => ReceiveLoopAsync(connection, linkedCancellation.Token), CancellationToken.None);
        }

        OnStateChanged();
    }

    public bool TryDequeueMessage(out string? message)
    {
        return _messageQueue.TryDequeue(out message);
    }

    /// <summary>
    /// Closes the connection. Raises <see cref="StateChanged"/> only when there was a connection or an error
    /// to clear; disconnecting an idle client is silent.
    /// </summary>
    public Task DisconnectAsync() => DisconnectCoreAsync(notify: true);

    private async Task DisconnectCoreAsync(bool notify)
    {
        ClientWebSocket? client;
        CancellationTokenSource? cancellation;
        bool hadState;

        lock (_sync)
        {
            hadState = _client is not null || _connection is not null || _lastError is not null;
            client = _client;
            cancellation = _receiveLoopCancellation;
            _client = null;
            _connection = null;
            _receiveLoopCancellation = null;
            _lastError = null;
        }

        cancellation?.Cancel();

        if (client is not null)
        {
            try
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch
            {
            }

            client.Dispose();
        }

        cancellation?.Dispose();
        if (notify && hadState)
        {
            OnStateChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
    }

    private async Task ReceiveLoopAsync(ModernWebSocketConnection connection, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];
        var builder = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && connection.IsOpen)
            {
                builder.Clear();
                WebSocketReceiveResult result;

                do
                {
                    result = await connection.WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync().ConfigureAwait(false);
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                _messageQueue.Enqueue(builder.ToString());
                OnStateChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException ex)
        {
            _lastError = $"WebSocket error: {ex.Message}";
            OnStateChanged();
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            OnStateChanged();
        }
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
