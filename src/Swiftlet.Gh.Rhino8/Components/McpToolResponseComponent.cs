using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class McpToolResponseComponent : GH_Component
{
    public McpToolResponseComponent()
        : base("MCP Tool Response", "Respond", "Sends the result of an MCP tool call back to the client. Return one or more content blocks, optional structured JSON data, and optionally mark the result as an error.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.senary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new McpToolCallRequestParam(), "Request", "R", "Original request from Deconstruct Tool Call. Each request can be answered only once.", GH_ParamAccess.item);
        pManager.AddParameter(new McpContentBlockParam(), "Content Blocks", "C", "Content blocks to return to the MCP client, such as text, images, resource links, or embedded resources.", GH_ParamAccess.list);
        pManager.AddParameter(new JsonObjectParam(), "Structured Content", "S", "Optional machine-readable JSON result. This can be returned together with content blocks.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Is Error", "E", "If true, marks the tool result as an error while still returning the provided content and/or structured data.", GH_ParamAccess.item, false);
        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Success", "OK", "True if the response was sent successfully.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        McpToolCallRequestGoo? requestGoo = null;
        List<McpContentBlockGoo> contentBlockGoos = [];
        JsonObjectGoo? structuredContentGoo = null;
        bool isError = false;

        if (!DA.GetData(0, ref requestGoo))
        {
            DA.SetData(0, false);
            return;
        }

        DA.GetDataList(1, contentBlockGoos);
        DA.GetData(2, ref structuredContentGoo);
        DA.GetData(3, ref isError);

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

        List<McpContentBlock> contentBlocks = contentBlockGoos
            .Where(static goo => goo?.Value is not null)
            .Select(static goo => goo!.Value!)
            .ToList();

        JsonObject? structuredContent = structuredContentGoo?.Value;
        if (contentBlocks.Count == 0 && structuredContent is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide at least one content block or structured content.");
            DA.SetData(0, false);
            return;
        }

        bool success = ModernMcpResponseWorkflow.TrySendResponse(
            requestGoo.Value,
            contentBlocks,
            structuredContent,
            isError);

        if (!success)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to send response");
        }

        DA.SetData(0, success);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("8F6B9587-D230-4CEB-8248-7DE136FED7AD");
}
