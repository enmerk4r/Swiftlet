using System.Text.Json.Nodes;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernMcpToolCallContext
{
    private readonly Func<McpToolResult, bool> _resultSender;
    private readonly object _responseSync = new();
    private bool _hasResponded;

    public ModernMcpToolCallContext(
        string callId,
        string toolName,
        JsonObject arguments,
        Func<McpToolResult, bool> resultSender)
    {
        CallId = string.IsNullOrWhiteSpace(callId)
            ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(callId))
            : callId;
        ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
        Arguments = arguments is null
            ? throw new ArgumentNullException(nameof(arguments))
            : JsonNodeCloner.CloneObject(arguments);
        _resultSender = resultSender ?? throw new ArgumentNullException(nameof(resultSender));
    }

    public string CallId { get; }

    public string ToolName { get; }

    public JsonObject Arguments { get; }

    public bool HasResponded => _hasResponded;

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

        lock (_responseSync)
        {
            if (_hasResponded)
            {
                return false;
            }

            bool sent = _resultSender(result.Duplicate());
            if (sent)
            {
                _hasResponded = true;
            }

            return sent;
        }
    }

    public bool TryRespondWithError(int code, string message)
    {
        return TryRespondWithToolResult(new McpToolResult(
            [new McpTextContentBlock(message ?? string.Empty)],
            isError: true));
    }
}
