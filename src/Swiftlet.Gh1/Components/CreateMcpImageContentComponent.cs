using Grasshopper.Kernel;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;
using Swiftlet.Imaging;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMcpImageContentComponent : GH_Component
{
    public CreateMcpImageContentComponent()
        : base("Create MCP Image Content", "MCP IMG", "Creates an image content block for MCP Tool Response by encoding a Swiftlet bitmap.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Bitmap to embed in the MCP tool result.", GH_ParamAccess.item);
        pManager.AddTextParameter("Format", "F", "Image encoding format to use for the embedded image. Supported: PNG, JPEG, BMP, GIF, TIFF.", GH_ParamAccess.item, "png");
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpContentBlockParam(), "Content Block", "C", "Image content block for MCP Tool Response.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        BitmapGoo? bitmapGoo = null;
        string format = "png";

        if (!DA.GetData(0, ref bitmapGoo) || bitmapGoo?.Value is null)
        {
            return;
        }

        DA.GetData(1, ref format);

        DA.SetData(0, new McpContentBlockGoo(McpContentFactory.CreateImage(bitmapGoo.Value, format)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B33BEF39-9140-4881-A562-EAC060946C5D");
}
