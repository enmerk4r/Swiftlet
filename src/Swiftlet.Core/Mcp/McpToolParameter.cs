namespace Swiftlet.Core.Mcp;

public sealed class McpToolParameter
{
    public McpToolParameter(string name, string type, string description, bool required = true)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = NormalizeType(type);
        Description = description ?? string.Empty;
        Required = required;
    }

    public string Name { get; }

    public string Type { get; }

    public string Description { get; }

    public bool Required { get; }

    public McpToolParameter Duplicate() => new(Name, Type, Description, Required);

    private static string NormalizeType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "string" => "string",
            "double" => "number",
            "float" => "number",
            "decimal" => "number",
            "number" => "number",
            "int" => "integer",
            "int32" => "integer",
            "int64" => "integer",
            "long" => "integer",
            "integer" => "integer",
            "bool" => "boolean",
            "boolean" => "boolean",
            "object" => "object",
            "array" => "array",
            _ => "string",
        };
    }
}
