using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public sealed class McpToolDefinition
{
    public McpToolDefinition(string name, string description, IEnumerable<McpToolParameter>? parameters = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        Parameters = parameters?.Select(static parameter => parameter.Duplicate()).ToArray() ?? [];
    }

    public string Name { get; }

    public string Description { get; }

    public IReadOnlyList<McpToolParameter> Parameters { get; }

    public McpToolDefinition Duplicate() => new(Name, Description, Parameters);

    public JsonObject ToInputSchema()
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (McpToolParameter parameter in Parameters)
        {
            var propertySchema = new JsonObject
            {
                ["type"] = parameter.Type,
            };

            if (!string.IsNullOrWhiteSpace(parameter.Description))
            {
                propertySchema["description"] = parameter.Description;
            }

            properties[parameter.Name] = propertySchema;

            if (parameter.Required)
            {
                required.Add(parameter.Name);
            }
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }

    public JsonObject ToJson()
    {
        return new JsonObject
        {
            ["name"] = Name,
            ["description"] = Description,
            ["inputSchema"] = ToInputSchema(),
        };
    }
}
