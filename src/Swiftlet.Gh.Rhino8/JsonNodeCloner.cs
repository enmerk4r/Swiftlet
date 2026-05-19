using System.Text.Json.Nodes;

namespace Swiftlet.Gh.Rhino8;

internal static class JsonNodeCloner
{
    public static JsonNode? Clone(JsonNode? node)
    {
        return node is null ? null : JsonNode.Parse(node.ToJsonString());
    }

    public static JsonObject CloneObject(JsonObject node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return (JsonNode.Parse(node.ToJsonString()) as JsonObject) ?? [];
    }
}
