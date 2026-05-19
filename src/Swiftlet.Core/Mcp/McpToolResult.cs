using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public sealed class McpToolResult
{
    public McpToolResult(
        IEnumerable<McpContentBlock>? content = null,
        JsonObject? structuredContent = null,
        bool isError = false)
    {
        Content = content?.Select(static block => block.Duplicate()).ToArray() ?? [];
        StructuredContent = CloneObject(structuredContent);
        IsError = isError;
    }

    public IReadOnlyList<McpContentBlock> Content { get; }

    public JsonObject? StructuredContent { get; }

    public bool IsError { get; }

    public McpToolResult Duplicate() => new(Content, StructuredContent, IsError);

    public JsonObject ToJson()
    {
        var result = new JsonObject
        {
            ["content"] = new JsonArray(Content.Select(static block => (JsonNode?)block.ToJson()).ToArray()),
        };

        if (StructuredContent is not null)
        {
            result["structuredContent"] = CloneObject(StructuredContent);
        }

        if (IsError)
        {
            result["isError"] = true;
        }

        return result;
    }

    private static JsonObject? CloneObject(JsonObject? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonNode.Parse(value.ToJsonString())?.AsObject()
            ?? throw new InvalidOperationException("Failed to clone JSON object.");
    }
}
