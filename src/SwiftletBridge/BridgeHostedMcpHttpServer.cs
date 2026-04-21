using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SwiftletBridge;

internal sealed class BridgeHostedMcpHttpServer : IAsyncDisposable
{
    private const string ProtocolVersion = "2024-11-05";

    private readonly Func<JsonObject, Task> _sendToPluginAsync;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonObject>> _pendingResults = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly int _port;

    private WebApplication? _app;
    private string _serverName = "Swiftlet";
    private JsonArray _tools = [];

    public BridgeHostedMcpHttpServer(int port, Func<JsonObject, Task> sendToPluginAsync)
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
            ApplicationName = typeof(BridgeHostedMcpHttpServer).Assembly.FullName,
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

    public void Configure(string serverName, JsonArray? tools)
    {
        lock (_sync)
        {
            _serverName = string.IsNullOrWhiteSpace(serverName) ? "Swiftlet" : serverName;
            _tools = CloneArray(tools) ?? [];
        }
    }

    public bool TryCompleteToolResult(string callId, JsonObject result)
    {
        if (string.IsNullOrWhiteSpace(callId) || result is null)
        {
            return false;
        }

        if (!_pendingResults.TryRemove(callId, out TaskCompletionSource<JsonObject>? source))
        {
            return false;
        }

        return source.TrySetResult(CloneObject(result));
    }

    public async Task StopAsync()
    {
        WebApplication? app = _app;
        _app = null;

        foreach ((string callId, TaskCompletionSource<JsonObject> source) in _pendingResults.ToArray())
        {
            if (_pendingResults.TryRemove(callId, out _))
            {
                source.TrySetResult(CreateToolErrorResult("Grasshopper MCP session stopped."));
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
        if (!context.Request.Path.StartsWithSegments("/mcp"))
        {
            context.Response.StatusCode = 404;
            return;
        }

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.StatusCode = 405;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Method Not Allowed").ConfigureAwait(false);
            return;
        }

        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        JsonObject? request = ParseJsonObject(body);
        if (request is null)
        {
            await WriteJsonResponseAsync(context, CreateJsonRpcError(null, -32700, "Parse error")).ConfigureAwait(false);
            return;
        }

        string? method = request["method"]?.GetValue<string>();
        JsonNode? requestId = CloneNode(request["id"]);
        JsonObject paramsObject = request["params"] as JsonObject ?? [];
        if (method is "notifications/initialized" or "initialized")
        {
            context.Response.StatusCode = 202;
            return;
        }

        JsonObject response = method switch
        {
            "initialize" => HandleInitialize(requestId, context.Response.Headers),
            "tools/list" => HandleToolsList(requestId),
            "tools/call" => await HandleToolsCallAsync(requestId, paramsObject, context.RequestAborted).ConfigureAwait(false),
            "ping" => CreateJsonRpcResult(requestId, new JsonObject()),
            _ => CreateJsonRpcError(requestId, -32601, $"Method not found: {method}"),
        };

        await WriteJsonResponseAsync(context, response).ConfigureAwait(false);
    }

    private JsonObject HandleInitialize(JsonNode? requestId, IHeaderDictionary headers)
    {
        headers["Mcp-Session-Id"] = Guid.NewGuid().ToString();

        string serverName;
        lock (_sync)
        {
            serverName = _serverName;
        }

        var result = new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject(),
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = serverName,
                ["version"] = "1.0.0",
            },
        };

        return CreateJsonRpcResult(requestId, result);
    }

    private JsonObject HandleToolsList(JsonNode? requestId)
    {
        JsonArray tools;
        lock (_sync)
        {
            tools = CloneArray(_tools) ?? [];
        }

        var result = new JsonObject
        {
            ["tools"] = tools,
        };

        return CreateJsonRpcResult(requestId, result);
    }

    private async Task<JsonObject> HandleToolsCallAsync(
        JsonNode? requestId,
        JsonObject paramsObject,
        CancellationToken cancellationToken)
    {
        string? toolName = paramsObject["name"]?.GetValue<string>();
        JsonObject arguments = paramsObject["arguments"] as JsonObject ?? [];

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return CreateJsonRpcError(requestId, -32602, "Missing tool name");
        }

        JsonArray toolsSnapshot;
        lock (_sync)
        {
            toolsSnapshot = CloneArray(_tools) ?? [];
        }

        bool toolExists = toolsSnapshot
            .OfType<JsonObject>()
            .Any(tool => string.Equals(tool["name"]?.GetValue<string>(), toolName, StringComparison.Ordinal));

        if (!toolExists)
        {
            return CreateJsonRpcError(requestId, -32602, $"Unknown tool: {toolName}");
        }

        string callId = Guid.NewGuid().ToString("N");
        var resultSource = new TaskCompletionSource<JsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingResults[callId] = resultSource;

        try
        {
            await _sendToPluginAsync(new JsonObject
            {
                ["type"] = "call_tool",
                ["callId"] = callId,
                ["toolName"] = toolName,
                ["arguments"] = CloneObject(arguments),
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _pendingResults.TryRemove(callId, out _);
            return CreateJsonRpcResult(requestId, CreateToolErrorResult($"Failed to dispatch tool call: {ex.Message}"));
        }

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromMinutes(2));
            JsonObject result = await resultSource.Task.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
            return CreateJsonRpcResult(requestId, result);
        }
        catch (OperationCanceledException)
        {
            _pendingResults.TryRemove(callId, out _);
            return CreateJsonRpcResult(requestId, CreateToolErrorResult("Timed out waiting for Grasshopper response."));
        }
    }

    private static async Task WriteJsonResponseAsync(HttpContext context, JsonObject response)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(response.ToJsonString()).ConfigureAwait(false);
    }

    private static JsonObject CreateJsonRpcResult(JsonNode? requestId, JsonNode result)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = CloneNode(requestId),
            ["result"] = result,
        };
    }

    private static JsonObject CreateJsonRpcError(JsonNode? requestId, int code, string message)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = CloneNode(requestId),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
            },
        };
    }

    private static JsonObject CreateToolErrorResult(string message)
    {
        return new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = message ?? string.Empty,
                },
            },
            ["isError"] = true,
        };
    }

    private static JsonObject? ParseJsonObject(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonNode? CloneNode(JsonNode? node)
    {
        return node?.DeepClone();
    }

    private static JsonObject CloneObject(JsonObject value)
    {
        return JsonNode.Parse(value.ToJsonString())?.AsObject()
            ?? throw new InvalidOperationException("Failed to clone JSON object.");
    }

    private static JsonArray? CloneArray(JsonArray? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonNode.Parse(value.ToJsonString()) as JsonArray;
    }
}
