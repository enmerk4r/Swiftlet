using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketServer
{
    private readonly ConcurrentDictionary<string, ModernWebSocketConnection> _activeConnections = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Task> _receiveTasks = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<ModernWebSocketReceivedMessage> _messageQueue = new();

    public event EventHandler? StateChanged;

    public int ActiveClientCount => _activeConnections.Count;

    public async Task<ModernWebSocketConnection> AcceptConnectionAsync(
        WebSocket webSocket,
        string remoteEndpoint,
        string localEndpoint,
        CancellationToken cancellationToken = default)
    {
        var connection = new ModernWebSocketConnection(webSocket, true, remoteEndpoint, localEndpoint);
        _activeConnections[connection.ConnectionId] = connection;
        _receiveTasks[connection.ConnectionId] = Task.Run(
            () => ReceiveLoopAsync(connection, cancellationToken),
            CancellationToken.None);

        OnStateChanged();
        await Task.CompletedTask;
        return connection;
    }

    public bool TryDequeueMessage(out ModernWebSocketReceivedMessage? message)
    {
        return _messageQueue.TryDequeue(out message);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        foreach (ModernWebSocketConnection connection in _activeConnections.Values)
        {
            try
            {
                if (connection.WebSocket.State == WebSocketState.Open)
                {
                    await connection.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server stopping",
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }

        _activeConnections.Clear();
        _receiveTasks.Clear();
        OnStateChanged();
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
                        RemoveConnection(connection.ConnectionId);
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                _messageQueue.Enqueue(new ModernWebSocketReceivedMessage(connection, builder.ToString()));
                OnStateChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            RemoveConnection(connection.ConnectionId);
        }
    }

    private void RemoveConnection(string connectionId)
    {
        _activeConnections.TryRemove(connectionId, out _);
        _receiveTasks.TryRemove(connectionId, out _);
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
