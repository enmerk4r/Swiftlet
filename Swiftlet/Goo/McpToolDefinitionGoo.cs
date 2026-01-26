using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;

namespace Swiftlet.Goo
{
    public class McpToolDefinitionGoo : GH_Goo<McpToolDefinition>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "MCP Tool Definition";

        public override string TypeDescription => "A tool definition for an MCP server";

        public McpToolDefinitionGoo()
        {
            this.Value = null;
        }

        public McpToolDefinitionGoo(McpToolDefinition definition)
        {
            this.Value = definition?.Duplicate();
        }

        public override IGH_Goo Duplicate()
        {
            return new McpToolDefinitionGoo(this.Value);
        }

        public override string ToString()
        {
            if (this.Value == null)
                return "Null MCP Tool";

            return $"MCP Tool: {this.Value.Name}";
        }
    }
}
