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

[IoId(ComponentIds.DeconstructToolCall)]
public sealed class DeconstructToolCallComponent : SwiftletComponent
{
    public DeconstructToolCallComponent() : base("Deconstruct Tool Call","Breaks an incoming MCP tool call into the original request, the tool name, and the JSON arguments sent by the MCP client.",Sections.Mcp) { }
    public DeconstructToolCallComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new McpToolCallRequestParameter("Request","R","Pending tool-call request emitted by MCP Server.",Access.Item));
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new McpToolCallRequestParameter("Request","R","Pass-through request object to send into MCP Tool Response after your Grasshopper logic finishes.",Access.Item));
        outputs.AddText("Tool","T","Name of the tool that the MCP client called.",Access.Item);
        outputs.Add(new JsonObjectParameter("Arguments","A","Arguments supplied by the MCP client as a JSON object.",Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        try
        {
            if(!access.GetItem(0,out ModernMcpToolCallContext request)||request is null){access.AddWarning("Invalid request","No valid request provided");return;}
        var result=ModernMcpResponseWorkflow.Deconstruct(request);access.SetItem(0,result.Request);access.SetItem(1,result.ToolName);
        access.SetItem(2,JsonObjectData.Parse(result.Arguments.ToJsonString()));
        }
        catch(OperationCanceledException) when(access.Solution.Token.IsCancellationRequested){throw;}
        catch(Exception error){access.AddError("Operation failed",error.Message);}
    }
}
