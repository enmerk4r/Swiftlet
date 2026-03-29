using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public sealed class McpEmbeddedBinaryResourceContentBlock : McpContentBlock
{
    private readonly byte[] _bytes;

    public McpEmbeddedBinaryResourceContentBlock(string uri, byte[] bytes, string? mimeType = null)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Resource URI is required.", nameof(uri));
        }

        Uri = uri;
        _bytes = bytes?.ToArray() ?? [];
        MimeType = mimeType;
    }

    public string Uri { get; }

    public string? MimeType { get; }

    public byte[] GetBytes() => _bytes.ToArray();

    public override McpContentBlock Duplicate() => new McpEmbeddedBinaryResourceContentBlock(Uri, _bytes, MimeType);

    public override JsonObject ToJson()
    {
        var resource = new JsonObject
        {
            ["uri"] = Uri,
            ["blob"] = Convert.ToBase64String(_bytes),
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
