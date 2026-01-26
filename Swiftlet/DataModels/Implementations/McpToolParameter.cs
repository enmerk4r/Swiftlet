using System;

namespace Swiftlet.DataModels.Implementations
{
    public class McpToolParameter
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Description { get; private set; }
        public bool Required { get; private set; }

        public McpToolParameter(string name, string type, string description, bool required = true)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Type = ValidateType(type);
            this.Description = description ?? string.Empty;
            this.Required = required;
        }

        private static string ValidateType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return "string";

            string normalizedType = type.ToLowerInvariant();

            switch (normalizedType)
            {
                case "string":
                case "number":
                case "integer":
                case "boolean":
                case "object":
                case "array":
                    return normalizedType;
                default:
                    return "string";
            }
        }

        public McpToolParameter Duplicate()
        {
            return new McpToolParameter(this.Name, this.Type, this.Description, this.Required);
        }
    }
}
