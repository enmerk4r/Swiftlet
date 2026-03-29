using Grasshopper.Kernel;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMcpTextContentComponent : GH_Component
{
    public CreateMcpTextContentComponent()
        : base("Create MCP Text Content", "MCP TXT", "Creates a text content block for MCP Tool Response.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Text to show to the MCP client as part of the tool result.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpContentBlockParam(), "Content Block", "C", "Text content block for MCP Tool Response.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string text = string.Empty;
        if (!DA.GetData(0, ref text))
        {
            return;
        }

        DA.SetData(0, new McpContentBlockGoo(new McpTextContentBlock(text)));
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override Guid ComponentGuid => new("6C9813A9-AAF8-4216-B33D-43EC2AE0AAE2");
}
