using Grasshopper.Kernel;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMcpResourceLinkComponent : GH_Component
{
    public CreateMcpResourceLinkComponent()
        : base("Create MCP Resource Link", "MCP LINK", "Creates a resource-link content block for MCP Tool Response. Use this when you want to point the client to a resource by URI instead of embedding the resource itself.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URI", "U", "URI of the linked resource.", GH_ParamAccess.item);
        pManager.AddTextParameter("Name", "N", "Short name for the linked resource.", GH_ParamAccess.item);
        pManager.AddTextParameter("Title", "T", "Optional display title for the linked resource.", GH_ParamAccess.item);
        pManager.AddTextParameter("Description", "D", "Optional plain-language summary of what the linked resource contains.", GH_ParamAccess.item);
        pManager.AddTextParameter("Mime Type", "M", "Optional MIME type of the linked resource, for example 'image/png' or 'application/json'.", GH_ParamAccess.item);
        pManager[2].Optional = true;
        pManager[3].Optional = true;
        pManager[4].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpContentBlockParam(), "Content Block", "C", "Resource-link content block for MCP Tool Response.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string uri = string.Empty;
        string name = string.Empty;
        string title = string.Empty;
        string description = string.Empty;
        string mimeType = string.Empty;

        if (!DA.GetData(0, ref uri) || !DA.GetData(1, ref name))
        {
            return;
        }

        DA.GetData(2, ref title);
        DA.GetData(3, ref description);
        DA.GetData(4, ref mimeType);

        DA.SetData(0, new McpContentBlockGoo(
            new McpResourceLinkContentBlock(uri, name, title, description, mimeType)));
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override Guid ComponentGuid => new("93033C5E-ABDE-4D99-9E9E-F06D79D55D12");
}
