using Grasshopper2.Components;
using Grasshopper2.Parameters;
using GrasshopperIO;
using Swiftlet.Core.Json;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

/// <summary>Exact three-pin compatibility adapter for saved pre-typed-content definitions.</summary>
[Obsolete("Compatibility component for legacy MCP JSON responses; use typed MCP Tool Response for new graphs.")]
[IoId(ComponentIds.McpToolResponseArchived)]
public sealed class McpToolResponseComponent_ARCHIVED : SwiftletComponent
{
    public McpToolResponseComponent_ARCHIVED() : base("MCP Tool Response", "Sends a response back to the MCP client for a tool call.", Sections.Mcp) { }
    public McpToolResponseComponent_ARCHIVED(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new McpToolCallRequestParameter("Request", "R", "The tool call request from MCP Server"));
        inputs.Add(new JsonNodeParameter("Content", "C", "Response content (JToken)"));
        inputs.AddBoolean("Is Error", "E", "Whether this is an error response").Set(false);
    }
    protected override void AddOutputs(OutputAdder outputs)
        => outputs.AddBoolean("Success", "OK", "True if the response was sent successfully");
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        if (!access.GetItem(0, out ModernMcpToolCallContext request) || request is null) { access.SetItem(0, false); return; }
        access.GetItem(1, out JsonToken content);
        access.GetItem(2, out bool isError);
        if (request.HasResponded) { access.AddWarning("Already responded", "Response has already been sent for this call"); access.SetItem(0, false); return; }
        bool success = ModernMcpResponseWorkflow.TrySendResponse(request, content?.ToNode(), isError);
        if (!success) access.AddError("Response failed", "Failed to send response");
        access.SetItem(0, success);
    }
}
