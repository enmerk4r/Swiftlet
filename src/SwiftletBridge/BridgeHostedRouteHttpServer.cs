using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SwiftletBridge;

internal sealed class BridgeHostedRouteHttpServer : IAsyncDisposable
{
    private readonly Func<JsonObject, Task> _sendToPluginAsync;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BridgeHttpResponse>> _pendingResponses =
        new(StringComparer.Ordinal);
    private readonly int _port;

    private WebApplication? _app;

    public BridgeHostedRouteHttpServer(int port, Func<JsonObject, Task> sendToPluginAsync)
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
            ApplicationName = typeof(BridgeHostedRouteHttpServer).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });

        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options => options.ListenLocalhost(_port));

        WebApplication app = builder.Build();
        app.Run(HandleRequestAsync);

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        _app = app;
    }

    public bool TryCompleteResponse(
        string requestId,
        int statusCode,
        string? contentType,
        byte[] bodyBytes,
        JsonArray? headers)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return false;
        }

        if (!_pendingResponses.TryRemove(requestId, out TaskCompletionSource<BridgeHttpResponse>? source))
        {
            return false;
        }

        return source.TrySetResult(new BridgeHttpResponse(statusCode, contentType, bodyBytes, CloneArray(headers) ?? []));
    }

    public async Task StopAsync()
    {
        WebApplication? app = _app;
        _app = null;

        foreach ((string requestId, TaskCompletionSource<BridgeHttpResponse> source) in _pendingResponses.ToArray())
        {
            if (_pendingResponses.TryRemove(requestId, out _))
            {
                source.TrySetResult(new BridgeHttpResponse(
                    500,
                    "text/plain; charset=utf-8",
                    Encoding.UTF8.GetBytes("Bridge route server stopped."),
                    []));
            }
        }

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
        byte[] bodyBytes;
        using (var memoryStream = new MemoryStream())
        {
            await context.Request.Body.CopyToAsync(memoryStream, context.RequestAborted).ConfigureAwait(false);
            bodyBytes = memoryStream.ToArray();
        }

        string requestId = Guid.NewGuid().ToString("N");
        var responseSource = new TaskCompletionSource<BridgeHttpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingResponses[requestId] = responseSource;

        try
        {
            await _sendToPluginAsync(new JsonObject
            {
                ["type"] = "http_request",
                ["requestId"] = requestId,
                ["method"] = context.Request.Method,
                ["path"] = context.Request.Path.HasValue ? context.Request.Path.Value! : "/",
                ["headers"] = BuildHeadersArray(context.Request.Headers),
                ["queryParameters"] = BuildQueryArray(context.Request.Query),
                ["contentType"] = context.Request.ContentType,
                ["bodyBase64"] = Convert.ToBase64String(bodyBytes),
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _pendingResponses.TryRemove(requestId, out _);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(ex.ToString()).ConfigureAwait(false);
            return;
        }

        BridgeHttpResponse response;
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeoutSource.CancelAfter(TimeSpan.FromMinutes(2));
            response = await responseSource.Task.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _pendingResponses.TryRemove(requestId, out _);
            context.Response.StatusCode = 504;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Timed out waiting for Grasshopper response.").ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = response.StatusCode;
        if (!string.IsNullOrWhiteSpace(response.ContentType))
        {
            context.Response.ContentType = response.ContentType;
        }

        foreach (JsonObject header in response.Headers.OfType<JsonObject>())
        {
            string? key = header["key"]?.GetValue<string>();
            string? value = header["value"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(key))
            {
                context.Response.Headers.Append(key, value ?? string.Empty);
            }
        }

        if (response.BodyBytes.Length > 0)
        {
            await context.Response.Body.WriteAsync(response.BodyBytes, context.RequestAborted).ConfigureAwait(false);
        }
    }

    private static JsonArray BuildHeadersArray(IHeaderDictionary headers)
    {
        return new JsonArray(headers
            .SelectMany(static pair => pair.Value.Select(value => (JsonNode?)new JsonObject
            {
                ["key"] = pair.Key,
                ["value"] = value ?? string.Empty,
            }))
            .ToArray());
    }

    private static JsonArray BuildQueryArray(IQueryCollection query)
    {
        return new JsonArray(query
            .SelectMany(static pair => pair.Value.Select(value => (JsonNode?)new JsonObject
            {
                ["key"] = pair.Key,
                ["value"] = value ?? string.Empty,
            }))
            .ToArray());
    }

    private static JsonArray? CloneArray(JsonArray? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonNode.Parse(value.ToJsonString()) as JsonArray;
    }

    private sealed class BridgeHttpResponse
    {
        public BridgeHttpResponse(int statusCode, string? contentType, byte[] bodyBytes, JsonArray headers)
        {
            StatusCode = statusCode;
            ContentType = contentType;
            BodyBytes = bodyBytes ?? [];
            Headers = headers ?? [];
        }

        public int StatusCode { get; }

        public string? ContentType { get; }

        public byte[] BodyBytes { get; }

        public JsonArray Headers { get; }
    }
}
