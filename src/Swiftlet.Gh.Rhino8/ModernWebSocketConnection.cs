using System.Net.WebSockets;
using System.Text;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketConnection
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly WebSocket? _webSocket;
    private readonly Func<string, CancellationToken, Task<bool>>? _proxySendAsync;
    private readonly Func<WebSocketState>? _proxyStateProvider;
    private WebSocketState _proxyState = WebSocketState.None;

    public ModernWebSocketConnection(
        WebSocket webSocket,
        bool isServer,
        string remoteEndpoint,
        string localEndpoint)
    {
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        ConnectionId = Guid.NewGuid().ToString("N")[..8];
        IsServer = isServer;
        RemoteEndpoint = remoteEndpoint ?? string.Empty;
        LocalEndpoint = localEndpoint ?? string.Empty;
        ConnectedAt = DateTime.UtcNow;
    }

    public ModernWebSocketConnection(
        string connectionId,
        bool isServer,
        string remoteEndpoint,
        string localEndpoint,
        Func<string, CancellationToken, Task<bool>> proxySendAsync,
        Func<WebSocketState>? proxyStateProvider = null)
    {
        ConnectionId = string.IsNullOrWhiteSpace(connectionId)
            ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionId))
            : connectionId;
        IsServer = isServer;
        RemoteEndpoint = remoteEndpoint ?? string.Empty;
        LocalEndpoint = localEndpoint ?? string.Empty;
        ConnectedAt = DateTime.UtcNow;
        _proxySendAsync = proxySendAsync ?? throw new ArgumentNullException(nameof(proxySendAsync));
        _proxyStateProvider = proxyStateProvider;
        _proxyState = WebSocketState.Open;
    }

    public WebSocket WebSocket => _webSocket ?? throw new InvalidOperationException("This WebSocket connection is bridge-backed and does not expose a raw WebSocket.");

    public string ConnectionId { get; }

    public bool IsServer { get; }

    public string RemoteEndpoint { get; }

    public string LocalEndpoint { get; }

    public DateTime ConnectedAt { get; }

    public WebSocketState State => _webSocket?.State ?? _proxyStateProvider?.Invoke() ?? _proxyState;

    public bool IsOpen => State == WebSocketState.Open;

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
            if (_webSocket is not null)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
                return true;
            }

            if (_proxySendAsync is not null)
            {
                return await _proxySendAsync(message, cancellationToken).ConfigureAwait(false);
            }

            return false;
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

    internal void UpdateProxyState(WebSocketState state)
    {
        _proxyState = state;
    }

    public string GetStatusString()
    {
        return State switch
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
