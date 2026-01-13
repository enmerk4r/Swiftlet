using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Swiftlet.DataModels.Implementations
{
    public class McpToolDefinition
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public List<McpToolParameter> Parameters { get; private set; }

        public McpToolDefinition(string name, string description, IEnumerable<McpToolParameter> parameters = null)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Description = description ?? string.Empty;
            this.Parameters = parameters?.Select(p => p.Duplicate()).ToList() ?? new List<McpToolParameter>();
        }

        public JObject ToInputSchema()
        {
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject()
            };

            var requiredParams = new JArray();

            foreach (var param in Parameters)
            {
                var propSchema = new JObject
                {
                    ["type"] = param.Type
                };

                if (!string.IsNullOrEmpty(param.Description))
                {
                    propSchema["description"] = param.Description;
                }

                ((JObject)schema["properties"])[param.Name] = propSchema;

                if (param.Required)
                {
                    requiredParams.Add(param.Name);
                }
            }

            if (requiredParams.Count > 0)
            {
                schema["required"] = requiredParams;
            }

            return schema;
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["name"] = this.Name,
                ["description"] = this.Description,
                ["inputSchema"] = this.ToInputSchema()
            };
        }

        public McpToolDefinition Duplicate()
        {
            return new McpToolDefinition(this.Name, this.Description, this.Parameters);
        }
    }
}
