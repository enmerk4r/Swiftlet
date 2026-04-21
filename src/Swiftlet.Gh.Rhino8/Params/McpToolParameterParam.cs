using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class McpToolParameterParam : GH_Param<McpToolParameterGoo>
{
    public McpToolParameterParam()
        : base("MCP Tool Parameter", "P", "Definition of one MCP tool parameter.", ShellNaming.Category, ShellNaming.Mcp, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public override Guid ComponentGuid => new("C9D0E1F2-A3B4-5678-2345-789012345678");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

