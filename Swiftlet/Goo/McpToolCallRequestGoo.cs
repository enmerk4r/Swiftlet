using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;

namespace Swiftlet.Goo
{
    public class McpToolCallRequestGoo : GH_Goo<McpToolCallContext>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "MCP Tool Call Request";

        public override string TypeDescription => "Request for a pending MCP tool call";

        public McpToolCallRequestGoo()
        {
            this.Value = null;
        }

        public McpToolCallRequestGoo(McpToolCallContext context)
        {
            this.Value = context;
        }

        public override IGH_Goo Duplicate()
        {
            return new McpToolCallRequestGoo(this.Value);
        }

        public override string ToString()
        {
            if (this.Value == null)
                return "Null MCP Call Request";

            string status = this.Value.HasResponded ? "responded" : "pending";
            return $"MCP Call: {this.Value.ToolName} ({status})";
        }
    }
}
