using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public sealed class McpResourceLinkContentBlock : McpContentBlock
{
    public McpResourceLinkContentBlock(
        string uri,
        string name,
        string? title = null,
        string? description = null,
        string? mimeType = null)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Resource URI is required.", nameof(uri));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Resource name is required.", nameof(name));
        }

        Uri = uri;
        Name = name;
        Title = title;
        Description = description;
        MimeType = mimeType;
    }

    public string Uri { get; }

    public string Name { get; }

    public string? Title { get; }

    public string? Description { get; }

    public string? MimeType { get; }

    public override McpContentBlock Duplicate() => new McpResourceLinkContentBlock(Uri, Name, Title, Description, MimeType);

    public override JsonObject ToJson()
    {
        var result = new JsonObject
        {
            ["type"] = "resource_link",
            ["uri"] = Uri,
            ["name"] = Name,
        };

        if (!string.IsNullOrWhiteSpace(Title))
        {
            result["title"] = Title;
        }

        if (!string.IsNullOrWhiteSpace(Description))
        {
            result["description"] = Description;
        }

        if (!string.IsNullOrWhiteSpace(MimeType))
        {
            result["mimeType"] = MimeType;
        }

        return result;
    }
}
