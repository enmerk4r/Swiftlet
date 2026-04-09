using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SwiftletBridge;

internal sealed class BridgeHostedWebSocketServer : IAsyncDisposable
{
    private readonly Func<JsonObject, Task> _sendToPluginAsync;
    private readonly ConcurrentDictionary<string, BridgeSocketConnection> _connections = new(StringComparer.Ordinal);
    private readonly int _port;

    private WebApplication? _app;

    public BridgeHostedWebSocketServer(int port, Func<JsonObject, Task> sendToPluginAsync)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        _port = port;
        _sendToPluginAsync = sendToPluginAsync ?? throw new ArgumentNullException(nameof(sendToPluginAsync));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ApplicationName = typeof(BridgeHostedWebSocketServer).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });

        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options => options.ListenLocalhost(_port));

        WebApplication app = builder.Build();
        app.UseWebSockets();
        app.Run(HandleRequestAsync);

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        _app = app;
    }

    public async Task<bool> SendMessageAsync(string connectionId, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId) || string.IsNullOrEmpty(message))
        {
            return false;
        }

        if (!_connections.TryGetValue(connectionId, out BridgeSocketConnection? connection))
        {
            return false;
        }

        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await connection.WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task StopAsync()
    {
        WebApplication? app = _app;
        _app = null;

        foreach (BridgeSocketConnection connection in _connections.Values.ToArray())
        {
            try
            {
                if (connection.WebSocket.State == WebSocketState.Open)
                {
                    await connection.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }

        _connections.Clear();

        if (app is not null)
        {
            try
            {
                await app.StopAsync().ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await app.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task HandleRequestAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Expected a WebSocket upgrade request.").ConfigureAwait(false);
            return;
        }

        WebSocket socket;
        try
        {
            socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        }
        catch
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
            }

            return;
        }

        string connectionId = Guid.NewGuid().ToString("N")[..8];
        string remoteEndpoint = BuildEndpoint(context.Connection.RemoteIpAddress?.ToString(), context.Connection.RemotePort, "unknown");
        string localEndpoint = BuildEndpoint(context.Connection.LocalIpAddress?.ToString(), context.Connection.LocalPort, ":" + _port);

        var connection = new BridgeSocketConnection(connectionId, socket, remoteEndpoint, localEndpoint);
        _connections[connectionId] = connection;

        await SendStateAsync(connection, "Open").ConfigureAwait(false);

        try
        {
            await ReceiveLoopAsync(connection, context.RequestAborted).ConfigureAwait(false);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            await SendStateAsync(connection, socket.State == WebSocketState.Aborted ? "Aborted" : "Closed").ConfigureAwait(false);
        }
    }

    private async Task ReceiveLoopAsync(BridgeSocketConnection connection, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];
        var builder = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            builder.Clear();

            try
            {
                do
                {
                    result = await connection.WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);
            }
            catch
            {
                return;
            }

            await _sendToPluginAsync(new JsonObject
            {
                ["type"] = "ws_message",
                ["connectionId"] = connection.ConnectionId,
                ["remoteEndpoint"] = connection.RemoteEndpoint,
                ["localEndpoint"] = connection.LocalEndpoint,
                ["message"] = builder.ToString(),
            }).ConfigureAwait(false);
        }
    }

    private Task SendStateAsync(BridgeSocketConnection connection, string state)
    {
        return _sendToPluginAsync(new JsonObject
        {
            ["type"] = "ws_state",
            ["connectionId"] = connection.ConnectionId,
            ["remoteEndpoint"] = connection.RemoteEndpoint,
            ["localEndpoint"] = connection.LocalEndpoint,
            ["state"] = state,
            ["activeClientCount"] = _connections.Count,
        });
    }

    private static string BuildEndpoint(string? address, int port, string fallback)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return fallback;
        }

        return port > 0 ? $"{address}:{port}" : address;
    }

    private sealed class BridgeSocketConnection
    {
        public BridgeSocketConnection(string connectionId, WebSocket webSocket, string remoteEndpoint, string localEndpoint)
        {
            ConnectionId = connectionId;
            WebSocket = webSocket;
            RemoteEndpoint = remoteEndpoint;
            LocalEndpoint = localEndpoint;
        }

        public string ConnectionId { get; }

        public WebSocket WebSocket { get; }

        public string RemoteEndpoint { get; }

        public string LocalEndpoint { get; }
    }
}
