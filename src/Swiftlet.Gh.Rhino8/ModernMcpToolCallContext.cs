using System.Text.Json.Nodes;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernMcpToolCallContext
{
    private readonly TaskCompletionSource<ModernMcpHttpResponse> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ModernMcpToolCallContext(string? sessionId, JsonNode? requestId, string toolName, JsonObject arguments)
    {
        SessionId = sessionId;
        RequestId = JsonNodeCloner.Clone(requestId);
        ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
        Arguments = arguments is null
            ? throw new ArgumentNullException(nameof(arguments))
            : JsonNodeCloner.CloneObject(arguments);
    }

    public string? SessionId { get; }

    public JsonNode? RequestId { get; }

    public string ToolName { get; }

    public JsonObject Arguments { get; }

    public Task<ModernMcpHttpResponse> ResponseTask => _completionSource.Task;

    public bool HasResponded => _completionSource.Task.IsCompleted;

    public bool TryRespondWithText(string textContent)
    {
        return TryRespondWithToolResult(new McpToolResult(
            [new McpTextContentBlock(textContent ?? string.Empty)]));
    }

    public bool TryRespondWithJson(JsonNode jsonContent)
    {
        ArgumentNullException.ThrowIfNull(jsonContent);
        return TryRespondWithText(jsonContent.ToJsonString());
    }

    public bool TryRespondWithToolResult(McpToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return _completionSource.TrySetResult(CreateResultResponse(result));
    }

    public bool TryRespondWithError(int code, string message)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNodeCloner.Clone(RequestId),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message ?? string.Empty,
            },
        };

        return _completionSource.TrySetResult(ModernMcpHttpResponse.Json(
            response.ToJsonString()));
    }

    private ModernMcpHttpResponse CreateResultResponse(McpToolResult result)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNodeCloner.Clone(RequestId),
            ["result"] = result.ToJson(),
        };

        return ModernMcpHttpResponse.Json(response.ToJsonString());
    }
}
