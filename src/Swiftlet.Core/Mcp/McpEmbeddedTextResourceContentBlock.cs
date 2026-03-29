using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public sealed class McpEmbeddedTextResourceContentBlock : McpContentBlock
{
    public McpEmbeddedTextResourceContentBlock(string uri, string text, string? mimeType = null)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Resource URI is required.", nameof(uri));
        }

        Uri = uri;
        Text = text ?? string.Empty;
        MimeType = mimeType;
    }

    public string Uri { get; }

    public string Text { get; }

    public string? MimeType { get; }

    public override McpContentBlock Duplicate() => new McpEmbeddedTextResourceContentBlock(Uri, Text, MimeType);

    public override JsonObject ToJson()
    {
        var resource = new JsonObject
        {
            ["uri"] = Uri,
            ["text"] = Text,
        };

        if (!string.IsNullOrWhiteSpace(MimeType))
        {
            resource["mimeType"] = MimeType;
        }

        return new JsonObject
        {
            ["type"] = "resource",
            ["resource"] = resource,
        };
    }
}
