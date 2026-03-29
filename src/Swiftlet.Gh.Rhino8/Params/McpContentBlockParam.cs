using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class McpContentBlockParam : GH_Param<McpContentBlockGoo>
{
    public McpContentBlockParam()
        : base("MCP Content Block", "CB", "One content block to include in an MCP tool result.", ShellNaming.Category, ShellNaming.Mcp, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("1B8787E7-C778-457F-9022-78B776009DB6");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}
