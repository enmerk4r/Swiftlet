using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMcpEmbeddedBinaryResourceComponent : GH_Component
{
    public CreateMcpEmbeddedBinaryResourceComponent()
        : base("Create MCP Embedded Binary Resource", "MCP EBIN", "Creates an embedded binary-resource content block for MCP Tool Response. Use this for file-like binary data that should be returned directly in the tool result.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URI", "U", "URI that identifies this embedded binary resource.", GH_ParamAccess.item);
        pManager.AddParameter(new ByteArrayParam(), "Bytes", "B", "Binary payload to embed in the resource.", GH_ParamAccess.item);
        pManager.AddTextParameter("Mime Type", "M", "MIME type of the embedded binary resource. Default: application/octet-stream.", GH_ParamAccess.item, ContentTypes.ApplicationOctetStream);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpContentBlockParam(), "Content Block", "C", "Embedded binary-resource content block for MCP Tool Response.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string uri = string.Empty;
        ByteArrayGoo? bytesGoo = null;
        string mimeType = ContentTypes.ApplicationOctetStream;

        if (!DA.GetData(0, ref uri) || !DA.GetData(1, ref bytesGoo) || bytesGoo?.Value is null)
        {
            return;
        }

        DA.GetData(2, ref mimeType);

        DA.SetData(0, new McpContentBlockGoo(
            new McpEmbeddedBinaryResourceContentBlock(uri, bytesGoo.Value, mimeType)));
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override Guid ComponentGuid => new("67DD0493-0140-454B-A8C0-AD7556E2DA80");
}
