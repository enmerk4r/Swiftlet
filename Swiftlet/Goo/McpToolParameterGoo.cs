using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;

namespace Swiftlet.Goo
{
    public class McpToolParameterGoo : GH_Goo<McpToolParameter>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "MCP Tool Parameter";

        public override string TypeDescription => "A parameter definition for an MCP tool";

        public McpToolParameterGoo()
        {
            this.Value = null;
        }

        public McpToolParameterGoo(McpToolParameter parameter)
        {
            this.Value = parameter?.Duplicate();
        }

        public override IGH_Goo Duplicate()
        {
            return new McpToolParameterGoo(this.Value);
        }

        public override string ToString()
        {
            if (this.Value == null)
                return "Null MCP Parameter";

            return $"MCP Param: {this.Value.Name} ({this.Value.Type})";
        }
    }
}
