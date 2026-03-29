using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public sealed class McpImageContentBlock : McpContentBlock
{
    public McpImageContentBlock(string mimeType, string data)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("Image MIME type is required.", nameof(mimeType));
        }

        MimeType = mimeType;
        Data = data ?? string.Empty;
    }

    public string MimeType { get; }

    public string Data { get; }

    public override McpContentBlock Duplicate() => new McpImageContentBlock(MimeType, Data);

    public override JsonObject ToJson()
    {
        return new JsonObject
        {
            ["type"] = "image",
            ["mimeType"] = MimeType,
            ["data"] = Data,
        };
    }
}
