using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Swiftlet.Gh.Rhino8;

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

    public async Task ConnectAsync(string url, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync().ConfigureAwait(false);

        var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri(url), cancellationToken).ConfigureAwait(false);

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

    public async Task DisconnectAsync()
    {
        ClientWebSocket? client;
        CancellationTokenSource? cancellation;

        lock (_sync)
        {
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
        OnStateChanged();
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
