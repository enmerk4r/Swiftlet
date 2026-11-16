using Grasshopper.Kernel.Types;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class McpToolDefinitionGoo : GH_Goo<McpToolDefinition>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "MCP Tool Definition";

    public override string TypeDescription => "A tool definition for an MCP server";

    public McpToolDefinitionGoo()
    {
        Value = default!;
    }

    public McpToolDefinitionGoo(McpToolDefinition? definition)
    {
        Value = definition is null ? default! : definition.Duplicate();
    }

    public override IGH_Goo Duplicate() => new McpToolDefinitionGoo(Value);

    public override string ToString() => Value is null ? "Null MCP Tool" : $"MCP Tool: {Value.Name}";
}
