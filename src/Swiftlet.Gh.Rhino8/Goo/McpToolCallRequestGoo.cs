using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class McpToolCallRequestGoo : GH_Goo<ModernMcpToolCallContext>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "MCP Tool Call Request";

    public override string TypeDescription => "Request for a pending MCP tool call";

    public McpToolCallRequestGoo()
    {
        Value = default!;
    }

    public McpToolCallRequestGoo(ModernMcpToolCallContext? context)
    {
        Value = context ?? default!;
    }

    public override IGH_Goo Duplicate() => new McpToolCallRequestGoo(Value);

    public override string ToString()
    {
        if (Value is null)
        {
            return "Null MCP Call Request";
        }

        string status = Value.HasResponded ? "responded" : "pending";
        return $"MCP Call: {Value.ToolName} ({status})";
    }
}
