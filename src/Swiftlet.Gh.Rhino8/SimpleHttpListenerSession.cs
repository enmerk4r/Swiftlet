using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

public sealed class SimpleHttpListenerSession : IAsyncDisposable
{
    private readonly object _sync = new();
    private WebApplication? _application;
    private string _route = "/";
    private IRequestBody? _responseBody;

    public event EventHandler? RequestReceived;

    public int Port { get; private set; }

    public string Route
    {
        get
        {
            lock (_sync)
            {
                return _route;
            }
        }
    }

    public SimpleHttpListenerRequest? LatestRequest { get; private set; }

    public bool IsRunning => _application is not null;

    public string StatusMessage
    {
        get
        {
            if (!IsRunning || Port <= 0)
            {
                return "Stopped";
            }

            string route = Route;
            return route == "/"
                ? $"http://localhost:{Port}/"
                : $"http://localhost:{Port}{route}/";
        }
    }

    public async Task ReconfigureAsync(
        int port,
        string? route,
        IRequestBody? responseBody,
        CancellationToken cancellationToken = default)
    {
        string normalizedRoute = ModernServerRouteMatcher.NormalizeRoute(route);

        lock (_sync)
        {
            _route = normalizedRoute;
            _responseBody = responseBody?.Duplicate();
        }

        if (Port == port && IsRunning)
        {
            return;
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
        application.Map("/{**path}", HandleRequestAsync);
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

    private async Task HandleRequestAsync(HttpContext context)
    {
        string configuredRoute;
        IRequestBody? responseBody;
        lock (_sync)
        {
            configuredRoute = _route;
            responseBody = _responseBody?.Duplicate();
        }

        string requestPath = ModernServerRouteMatcher.NormalizeRoute(context.Request.Path.Value ?? "/");
        string? matchedRoute = ModernServerRouteMatcher.FindBestMatch(requestPath, [configuredRoute]);
        if (matchedRoute is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = $"{ContentTypes.TextPlain}; charset=utf-8";
            await context.Response.WriteAsync("404 - Route not found", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        byte[] bodyBytes;
        using (var memoryStream = new MemoryStream())
        {
            await context.Request.Body.CopyToAsync(memoryStream, context.RequestAborted).ConfigureAwait(false);
            bodyBytes = memoryStream.ToArray();
        }

        var headers = context.Request.Headers
            .Select(static header => new HttpHeader(header.Key, header.Value.FirstOrDefault() ?? string.Empty))
            .ToArray();

        var queryParameters = context.Request.Query
            .Select(static parameter => new QueryParameter(parameter.Key, parameter.Value.FirstOrDefault() ?? string.Empty))
            .ToArray();

        LatestRequest = new SimpleHttpListenerRequest(
            requestPath,
            context.Request.Method,
            headers,
            queryParameters,
            Encoding.UTF8.GetString(bodyBytes));
        RequestReceived?.Invoke(this, EventArgs.Empty);

        byte[] responseBytes = responseBody?.ToByteArray() ?? [];
        if (!string.IsNullOrWhiteSpace(responseBody?.ContentType))
        {
            context.Response.ContentType = responseBody.ContentType;
        }

        if (responseBytes.Length > 0)
        {
            await context.Response.Body.WriteAsync(responseBytes, context.RequestAborted).ConfigureAwait(false);
        }
    }
}
