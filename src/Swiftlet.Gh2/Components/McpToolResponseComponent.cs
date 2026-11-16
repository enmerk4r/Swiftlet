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

[IoId(ComponentIds.McpToolResponse)]
public sealed class McpToolResponseComponent : SwiftletComponent
{
    public McpToolResponseComponent() : base("MCP Tool Response","Sends the result of an MCP tool call back to the client. Return one or more content blocks, optional structured JSON data, and optionally mark the result as an error.",Sections.Mcp) { }
    public McpToolResponseComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new McpToolCallRequestParameter("Request","R","Original request from Deconstruct Tool Call. Each request can be answered only once.",Access.Item));
        var p1=new McpContentBlockParameter("Content Blocks","C","Content blocks to return to the MCP client, such as text, images, resource links, or embedded resources.",Access.Twig) { Requirement = Requirement.MayBeMissing };p1.Set(Array.Empty<McpContentBlock>());inputs.Add(p1);
        inputs.Add(new JsonObjectParameter("Structured Content","S","Optional machine-readable JSON result. This can be returned together with content blocks.",Access.Item) { Requirement = Requirement.MayBeMissing });
        inputs.AddBoolean("Is Error","E","If true, marks the tool result as an error while still returning the provided content and/or structured data.",Access.Item).Set(false);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddBoolean("Success","OK","True if the response was sent successfully.",Access.Item);
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        try
        {
            if(!access.GetItem(0,out ModernMcpToolCallContext request)||request is null){access.SetItem(0,false);return;}
        access.GetTwig<McpContentBlock>(1,out var content);access.GetItem(2,out JsonObjectData structured);access.GetItem(3,out bool isError);
        if(request.HasResponded){access.AddWarning("Already responded","Response has already been sent for this call");access.SetItem(0,false);return;}
        var blocks=content?.NonNullItems.ToArray()??Array.Empty<McpContentBlock>();
        if(blocks.Length==0&&structured is null){access.AddError("Empty response","Provide at least one content block or structured content.");access.SetItem(0,false);return;}
        bool sent=ModernMcpResponseWorkflow.TrySendResponse(request,blocks,structured?.ToObject(),isError);
        if(!sent)access.AddError("Response failed","Failed to send response");access.SetItem(0,sent);
        }
        catch(OperationCanceledException) when(access.Solution.Token.IsCancellationRequested){throw;}
        catch(Exception error){access.AddError("Operation failed",error.Message);}
    }
}
