using Grasshopper2.Components;
using Grasshopper2.Parameters;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Core.Json;
using Swiftlet.Core.Mcp;
using Swiftlet.Imaging;
using Swiftlet.Runtime;
using Swiftlet.Gh2.Parameters;
namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.CreateMcpEmbeddedBinaryResource)]
public sealed class CreateMcpEmbeddedBinaryResourceComponent : SwiftletComponent
{
    public CreateMcpEmbeddedBinaryResourceComponent() : base("Create MCP Embedded Binary Resource","Creates an embedded binary-resource content block for MCP Tool Response. Use this for file-like binary data that should be returned directly in the tool result.",Sections.Mcp) { }
    public CreateMcpEmbeddedBinaryResourceComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URI","U","URI that identifies this embedded binary resource.",Access.Item);
        inputs.Add(new ByteArrayParameter("Bytes","B","Binary payload to embed in the resource.",Access.Item));
        inputs.AddText("Mime Type","M","MIME type of the embedded binary resource. Default: application/octet-stream.",Access.Item, Requirement.MayBeMissing).Set("application/octet-stream");
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new McpContentBlockParameter("Content Block","C","Embedded binary-resource content block for MCP Tool Response.",Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        try
        {
            access.GetItem(0, out string uri); if (!access.GetItem(1, out byte[] bytes) || bytes is null) return;
        access.GetItem(2, out string mime); access.SetItem(0, new McpEmbeddedBinaryResourceContentBlock(uri,bytes,mime));
        }
        catch(OperationCanceledException) when(access.Solution.Token.IsCancellationRequested){throw;}
        catch(Exception error){access.AddError("Operation failed",error.Message);}
    }
}
