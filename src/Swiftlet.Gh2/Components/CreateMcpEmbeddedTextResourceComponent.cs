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

[IoId(ComponentIds.CreateMcpEmbeddedTextResource)]
public sealed class CreateMcpEmbeddedTextResourceComponent : SwiftletComponent
{
    public CreateMcpEmbeddedTextResourceComponent() : base("Create MCP Embedded Text Resource","Creates an embedded text-resource content block for MCP Tool Response. Use this when the result is a file-like text artifact rather than plain message text.",Sections.Mcp) { }
    public CreateMcpEmbeddedTextResourceComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URI","U","URI that identifies this embedded text resource.",Access.Item);
        inputs.AddText("Text","T","Text payload to embed in the resource.",Access.Item);
        inputs.AddText("Mime Type","M","MIME type of the embedded text resource. Default: text/plain.",Access.Item, Requirement.MayBeMissing).Set("text/plain");
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new McpContentBlockParameter("Content Block","C","Embedded text-resource content block for MCP Tool Response.",Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        try
        {
            access.GetItem(0, out string uri); access.GetItem(1, out string text); access.GetItem(2, out string mime);
        access.SetItem(0, new McpEmbeddedTextResourceContentBlock(uri,text,mime));
        }
        catch(OperationCanceledException) when(access.Solution.Token.IsCancellationRequested){throw;}
        catch(Exception error){access.AddError("Operation failed",error.Message);}
    }
}
