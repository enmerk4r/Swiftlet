using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public sealed class McpTextContentBlock : McpContentBlock
{
    public McpTextContentBlock(string text)
    {
        Text = text ?? string.Empty;
    }

    public string Text { get; }

    public override McpContentBlock Duplicate() => new McpTextContentBlock(Text);

    public override JsonObject ToJson()
    {
        return new JsonObject
        {
            ["type"] = "text",
            ["text"] = Text,
        };
    }
}
