using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMcpEmbeddedTextResourceComponent : GH_Component
{
    public CreateMcpEmbeddedTextResourceComponent()
        : base("Create MCP Embedded Text Resource", "MCP ETXT", "Creates an embedded text-resource content block for MCP Tool Response. Use this when the result is a file-like text artifact rather than plain message text.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URI", "U", "URI that identifies this embedded text resource.", GH_ParamAccess.item);
        pManager.AddTextParameter("Text", "T", "Text payload to embed in the resource.", GH_ParamAccess.item);
        pManager.AddTextParameter("Mime Type", "M", "MIME type of the embedded text resource. Default: text/plain.", GH_ParamAccess.item, ContentTypes.TextPlain);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpContentBlockParam(), "Content Block", "C", "Embedded text-resource content block for MCP Tool Response.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string uri = string.Empty;
        string text = string.Empty;
        string mimeType = ContentTypes.TextPlain;

        if (!DA.GetData(0, ref uri) || !DA.GetData(1, ref text))
        {
            return;
        }

        DA.GetData(2, ref mimeType);

        DA.SetData(0, new McpContentBlockGoo(
            new McpEmbeddedTextResourceContentBlock(uri, text, mimeType)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("4E1E3FFB-0592-4DE3-A3AA-1ADB000526DA");
}
