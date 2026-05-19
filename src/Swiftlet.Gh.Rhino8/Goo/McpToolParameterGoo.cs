using Grasshopper.Kernel.Types;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class McpToolParameterGoo : GH_Goo<McpToolParameter>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "MCP Tool Parameter";

    public override string TypeDescription => "A parameter definition for an MCP tool";

    public McpToolParameterGoo()
    {
        Value = default!;
    }

    public McpToolParameterGoo(McpToolParameter? parameter)
    {
        Value = parameter is null ? default! : parameter.Duplicate();
    }

    public override IGH_Goo Duplicate() => new McpToolParameterGoo(Value);

    public override string ToString() => Value is null ? "Null MCP Parameter" : $"MCP Param: {Value.Name} ({Value.Type})";
}
