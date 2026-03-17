using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernMcpServerTransport : IAsyncDisposable
{
    private readonly ModernMcpServer _server;
    private WebApplication? _application;

    public ModernMcpServerTransport(ModernMcpServer server)
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

        WebApplication application = builder.Build();

        application.Map("/{**path}", async context =>
        {
            try
            {
                if (!context.Request.Path.StartsWithSegments("/mcp", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                string? sessionId = context.Request.Headers["Mcp-Session-Id"].FirstOrDefault();
                string body;
                using (var reader = new StreamReader(context.Request.Body))
                {
                    body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
                }

                ModernMcpHttpResponse response = await _server
                    .HandleHttpRequestAsync(context.Request.Method, sessionId, body, context.RequestAborted)
                    .ConfigureAwait(false);

                context.Response.StatusCode = response.StatusCode;

                if (!string.IsNullOrWhiteSpace(response.ContentType))
                {
                    context.Response.ContentType = response.ContentType;
                }

                foreach (KeyValuePair<string, string> header in response.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value;
                }

                if (!string.IsNullOrEmpty(response.Body))
                {
                    await context.Response.WriteAsync(response.Body, context.RequestAborted).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(ex.ToString(), context.RequestAborted).ConfigureAwait(false);
            }
        });

        await application.StartAsync(cancellationToken).ConfigureAwait(false);

        _application = application;
        Port = port;
    }

    public async Task StopAsync()
    {
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
}
