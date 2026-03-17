using System.Net.WebSockets;
using System.Text;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketConnection
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public ModernWebSocketConnection(
        WebSocket webSocket,
        bool isServer,
        string remoteEndpoint,
        string localEndpoint)
    {
        WebSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        ConnectionId = Guid.NewGuid().ToString("N")[..8];
        IsServer = isServer;
        RemoteEndpoint = remoteEndpoint ?? string.Empty;
        LocalEndpoint = localEndpoint ?? string.Empty;
        ConnectedAt = DateTime.UtcNow;
    }

    public WebSocket WebSocket { get; }

    public string ConnectionId { get; }

    public bool IsServer { get; }

    public string RemoteEndpoint { get; }

    public string LocalEndpoint { get; }

    public DateTime ConnectedAt { get; }

    public WebSocketState State => WebSocket.State;

    public bool IsOpen => WebSocket.State == WebSocketState.Open;

    public bool SendMessage(string message)
    {
        return SendMessageAsync(message).GetAwaiter().GetResult();
    }

    public async Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!IsOpen || string.IsNullOrEmpty(message))
        {
            return false;
        }

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public string GetStatusString()
    {
        return WebSocket.State switch
        {
            WebSocketState.None => "Not connected",
            WebSocketState.Connecting => "Connecting...",
            WebSocketState.Open => "Connected",
            WebSocketState.CloseSent => "Closing...",
            WebSocketState.CloseReceived => "Remote closing...",
            WebSocketState.Closed => "Closed",
            WebSocketState.Aborted => "Aborted",
            _ => "Unknown",
        };
    }

    public override string ToString()
    {
        string type = IsServer ? "Server" : "Client";
        return $"WebSocket {type} [{ConnectionId}] - {GetStatusString()}";
    }
}
