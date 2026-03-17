using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernMcpServer
{
    private const string ProtocolVersion = "2024-11-05";

    private readonly ConcurrentDictionary<string, SessionState> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ModernMcpToolCallContext>> _pendingCalls = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private IReadOnlyList<McpToolDefinition> _tools = [];

    public ModernMcpServer(string serverName, IEnumerable<McpToolDefinition>? tools = null)
    {
        ServerName = string.IsNullOrWhiteSpace(serverName) ? "Swiftlet" : serverName;
        UpdateTools(tools ?? []);
    }

    public event EventHandler? RequestQueued;

    public string ServerName { get; private set; }

    public IReadOnlyList<McpToolDefinition> Tools
    {
        get
        {
            lock (_sync)
            {
                return _tools;
            }
        }
    }

    public void Configure(string serverName, IEnumerable<McpToolDefinition>? tools)
    {
        lock (_sync)
        {
            ServerName = string.IsNullOrWhiteSpace(serverName) ? "Swiftlet" : serverName;
            _tools = tools?.Select(static tool => tool.Duplicate()).ToArray() ?? [];
        }
    }

    public void UpdateTools(IEnumerable<McpToolDefinition>? tools)
    {
        Configure(ServerName, tools);
    }

    public bool TryDequeuePendingCall(string toolName, out ModernMcpToolCallContext? context)
    {
        context = null;
        if (!_pendingCalls.TryGetValue(toolName, out ConcurrentQueue<ModernMcpToolCallContext>? queue))
        {
            return false;
        }

        return queue.TryDequeue(out context);
    }

    public async Task<ModernMcpHttpResponse> HandleHttpRequestAsync(
        string httpMethod,
        string? sessionId,
        string? body,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return ModernMcpHttpResponse.PlainText(405, "Method Not Allowed");
        }

        JsonObject? request;
        try
        {
            request = JsonNode.Parse(body ?? string.Empty) as JsonObject;
        }
        catch (JsonException)
        {
            request = null;
        }

        if (request is null)
        {
            return CreateJsonRpcError(null, -32700, "Parse error");
        }

        string? method = request["method"]?.GetValue<string>();
        JsonNode? requestId = JsonNodeCloner.Clone(request["id"]);
        JsonObject paramsObject = request["params"] as JsonObject ?? [];

        return method switch
        {
            "initialize" => HandleInitialize(requestId),
            "notifications/initialized" or "initialized" => HandleInitialized(sessionId),
            "tools/list" => HandleToolsList(requestId),
            "tools/call" => await HandleToolsCallAsync(requestId, paramsObject, sessionId, cancellationToken).ConfigureAwait(false),
            "ping" => CreateJsonRpcResult(requestId, new JsonObject()),
            _ => CreateJsonRpcError(requestId, -32601, $"Method not found: {method}"),
        };
    }

    private ModernMcpHttpResponse HandleInitialize(JsonNode? requestId)
    {
        string sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = new SessionState(sessionId);

        var result = new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject(),
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = ServerName,
                ["version"] = "1.0.0",
            },
        };

        return CreateJsonRpcResult(
            requestId,
            result,
            [new KeyValuePair<string, string>("Mcp-Session-Id", sessionId)]);
    }

    private ModernMcpHttpResponse HandleInitialized(string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId) && _sessions.TryGetValue(sessionId, out SessionState? session))
        {
            session.Initialized = true;
        }

        return ModernMcpHttpResponse.Accepted();
    }

    private ModernMcpHttpResponse HandleToolsList(JsonNode? requestId)
    {
        JsonArray toolsArray;
        lock (_sync)
        {
            toolsArray = new JsonArray(_tools.Select(static tool => tool.ToJson()).ToArray<JsonNode?>());
        }

        var result = new JsonObject
        {
            ["tools"] = toolsArray,
        };

        return CreateJsonRpcResult(requestId, result);
    }

    private async Task<ModernMcpHttpResponse> HandleToolsCallAsync(
        JsonNode? requestId,
        JsonObject paramsObject,
        string? sessionId,
        CancellationToken cancellationToken)
    {
        string? toolName = paramsObject["name"]?.GetValue<string>();
        JsonObject arguments = paramsObject["arguments"] as JsonObject ?? [];

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return CreateJsonRpcError(requestId, -32602, "Missing tool name");
        }

        bool toolExists;
        lock (_sync)
        {
            toolExists = _tools.Any(tool => string.Equals(tool.Name, toolName, StringComparison.Ordinal));
        }

        if (!toolExists)
        {
            return CreateJsonRpcError(requestId, -32602, $"Unknown tool: {toolName}");
        }

        var callContext = new ModernMcpToolCallContext(sessionId, requestId, toolName, arguments);
        ConcurrentQueue<ModernMcpToolCallContext> queue = _pendingCalls.GetOrAdd(toolName, static _ => new ConcurrentQueue<ModernMcpToolCallContext>());
        queue.Enqueue(callContext);
        RequestQueued?.Invoke(this, EventArgs.Empty);

        try
        {
            return await callContext.ResponseTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ModernMcpHttpResponse.PlainText(408, "Tool call cancelled");
        }
    }

    private static ModernMcpHttpResponse CreateJsonRpcResult(
        JsonNode? requestId,
        JsonNode result,
        IEnumerable<KeyValuePair<string, string>>? headers = null)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNodeCloner.Clone(requestId),
            ["result"] = result,
        };

        return ModernMcpHttpResponse.Json(
            response.ToJsonString(),
            headers);
    }

    private static ModernMcpHttpResponse CreateJsonRpcError(JsonNode? requestId, int code, string message)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNodeCloner.Clone(requestId),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
            },
        };

        return ModernMcpHttpResponse.Json(response.ToJsonString());
    }

    private sealed class SessionState
    {
        public SessionState(string id)
        {
            Id = id;
            Created = DateTime.UtcNow;
        }

        public string Id { get; }

        public DateTime Created { get; }

        public bool Initialized { get; set; }
    }
}
