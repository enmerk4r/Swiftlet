using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketServerTransport : IAsyncDisposable
{
    private readonly ModernWebSocketServer _server;
    private WebApplication? _application;

    public ModernWebSocketServerTransport(ModernWebSocketServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public bool IsRunning => _application is not null;

    public int Port { get; private set; }

    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        await StopAsync().ConfigureAwait(false);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1;
            });
        });

        WebApplication app = builder.Build();
        app.UseWebSockets();
        app.Run(HandleRequestAsync);

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        _application = app;
        Port = port;
    }

    public async Task StopAsync()
    {
        await _server.StopAsync().ConfigureAwait(false);

        if (_application is null)
        {
            return;
        }

        try
        {
            await _application.StopAsync().ConfigureAwait(false);
        }
        finally
        {
            await _application.DisposeAsync().ConfigureAwait(false);
            _application = null;
            Port = 0;
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
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using System.Net.WebSockets.WebSocket socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        string remoteEndpoint = context.Connection.RemoteIpAddress is null
            ? "unknown"
            : context.Connection.RemoteIpAddress + ":" + context.Connection.RemotePort;
        string localEndpoint = ":" + Port;

        await _server.AcceptConnectionAsync(socket, remoteEndpoint, localEndpoint, context.RequestAborted).ConfigureAwait(false);

        while (socket.State == System.Net.WebSockets.WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
        {
            await Task.Delay(50, context.RequestAborted).ConfigureAwait(false);
        }
    }
}
