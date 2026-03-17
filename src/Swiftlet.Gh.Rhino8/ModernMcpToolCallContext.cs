using System.Text.Json.Nodes;

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
        return _completionSource.TrySetResult(CreateResultResponse(textContent ?? string.Empty));
    }

    public bool TryRespondWithJson(JsonNode jsonContent)
    {
        ArgumentNullException.ThrowIfNull(jsonContent);
        return TryRespondWithText(jsonContent.ToJsonString());
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

    private ModernMcpHttpResponse CreateResultResponse(string textContent)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNodeCloner.Clone(RequestId),
            ["result"] = new JsonObject
            {
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = textContent,
                    },
                },
            },
        };

        return ModernMcpHttpResponse.Json(response.ToJsonString());
    }
}
