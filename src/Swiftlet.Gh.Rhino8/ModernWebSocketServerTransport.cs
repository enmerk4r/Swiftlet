using System.Net;
using System.Net.WebSockets;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketServerTransport : IAsyncDisposable
{
    private readonly ModernWebSocketServer _server;
    private HttpListener? _listener;
    private Task? _listenTask;

    public ModernWebSocketServerTransport(ModernWebSocketServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public bool IsRunning => _listener?.IsListening == true;

    public int Port { get; private set; }

    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        await StopAsync().ConfigureAwait(false);

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        _listener = listener;
        Port = port;
        _listenTask = Task.Run(() => ListenLoopAsync(listener));
    }

    public async Task StopAsync()
    {
        await _server.StopAsync().ConfigureAwait(false);

        HttpListener? listener = _listener;
        _listener = null;

        if (listener is not null)
        {
            try { listener.Stop(); }
            catch { }
            try { listener.Close(); }
            catch { }
        }

        if (_listenTask is not null)
        {
            try { await _listenTask.ConfigureAwait(false); }
            catch { }
            _listenTask = null;
        }

        Port = 0;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task ListenLoopAsync(HttpListener listener)
    {
        while (listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(context));
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        if (!context.Request.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        HttpListenerWebSocketContext wsContext;
        try
        {
            wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
        }
        catch
        {
            try { context.Response.Close(); }
            catch { }
            return;
        }

        WebSocket socket = wsContext.WebSocket;
        string remoteEndpoint = context.Request.RemoteEndPoint is not null
            ? context.Request.RemoteEndPoint.ToString()
            : "unknown";
        string localEndpoint = ":" + Port;

        await _server.AcceptConnectionAsync(socket, remoteEndpoint, localEndpoint, CancellationToken.None).ConfigureAwait(false);

        while (socket.State == WebSocketState.Open)
        {
            await Task.Delay(50).ConfigureAwait(false);
        }
    }
}
