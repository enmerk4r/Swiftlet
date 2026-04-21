using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class McpToolCallRequestParam : GH_Param<McpToolCallRequestGoo>
{
    public McpToolCallRequestParam()
        : base("MCP Tool Call Request", "R", "Pending MCP tool-call request.", ShellNaming.Category, ShellNaming.Mcp, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("E1F2A3B4-C5D6-7890-4567-901234567890");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

