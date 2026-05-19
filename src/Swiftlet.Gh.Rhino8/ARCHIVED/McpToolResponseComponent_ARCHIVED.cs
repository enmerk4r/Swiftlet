using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

[Obsolete("Archived legacy component. Use the current MCP Tool Response component for typed MCP content blocks.")]
public sealed class McpToolResponseComponent_ARCHIVED : GH_Component
{
    public McpToolResponseComponent_ARCHIVED()
        : base("MCP Tool Response", "Respond", "Sends a response back to the MCP client for a tool call.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new McpToolCallRequestParam(), "Request", "R", "The tool call request from MCP Server", GH_ParamAccess.item);
        pManager.AddParameter(new JsonNodeParam(), "Content", "C", "Response content (JToken)", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Is Error", "E", "Whether this is an error response", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Success", "OK", "True if the response was sent successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        McpToolCallRequestGoo? requestGoo = null;
        JsonNodeGoo? contentGoo = null;
        bool isError = false;

        if (!DA.GetData(0, ref requestGoo))
        {
            DA.SetData(0, false);
            return;
        }

        DA.GetData(1, ref contentGoo);
        DA.GetData(2, ref isError);

        if (requestGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid request provided");
            DA.SetData(0, false);
            return;
        }

        if (requestGoo.Value.HasResponded)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Response has already been sent for this call");
            DA.SetData(0, false);
            return;
        }

        bool success = ModernMcpResponseWorkflow.TrySendResponse(
            requestGoo.Value,
            contentGoo?.Value,
            isError);

        if (!success)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to send response");
        }

        DA.SetData(0, success);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B8C9D0E1-F2A3-4567-1234-678901234567");
}
