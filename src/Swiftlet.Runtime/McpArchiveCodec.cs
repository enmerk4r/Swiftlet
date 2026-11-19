using System.Text.Json.Nodes;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Runtime;

public static class McpArchiveCodec
{
    public static JsonObject Parameter(McpToolParameter parameter) => new()
    { ["name"] = parameter.Name, ["type"] = parameter.Type, ["description"] = parameter.Description, ["required"] = parameter.Required };
    public static McpToolParameter Parameter(JsonObject json) => new(
        (string)json["name"]!, (string)json["type"]!, (string?)json["description"] ?? "", (bool?)json["required"] ?? true);
    public static JsonObject Tool(McpToolDefinition tool) => new()
    {
        ["name"] = tool.Name,
        ["description"] = tool.Description,
        ["parameters"] = new JsonArray(tool.Parameters.Select(p => (JsonNode)Parameter(p)).ToArray())
    };
    public static McpToolDefinition Tool(JsonObject json) => new((string)json["name"]!, (string?)json["description"] ?? "",
        ((JsonArray)json["parameters"]!).Select(p => Parameter((JsonObject)p!)));
    public static McpContentBlock Content(JsonObject json)
    {
        string? type = (string?)json["type"];
        var resource = json["resource"] as JsonObject;
        return type switch
        {
            "text" => new McpTextContentBlock((string?)json["text"] ?? ""),
            "image" => new McpImageContentBlock((string)json["mimeType"]!, (string?)json["data"] ?? ""),
            "resource_link" => new McpResourceLinkContentBlock((string)json["uri"]!, (string)json["name"]!,
                (string?)json["title"], (string?)json["description"], (string?)json["mimeType"]),
            "resource" when resource?.ContainsKey("text") == true => new McpEmbeddedTextResourceContentBlock(
                (string)resource["uri"]!, (string?)resource["text"] ?? "", (string?)resource["mimeType"]),
            "resource" when resource is not null => new McpEmbeddedBinaryResourceContentBlock(
                (string)resource["uri"]!, Convert.FromBase64String((string?)resource["blob"] ?? ""), (string?)resource["mimeType"]),
            _ => throw new FormatException($"Unsupported MCP content type '{type}'.")
        };
    }
}
