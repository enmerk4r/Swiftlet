using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class McpToolDefinitionParam : GH_Param<McpToolDefinitionGoo>
{
    public McpToolDefinitionParam()
        : base("MCP Tool Definition", "T", "A tool definition for an MCP server", ShellNaming.Category, ShellNaming.Mcp, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public override Guid ComponentGuid => new("D0E1F2A3-B4C5-6789-3456-890123456789");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

