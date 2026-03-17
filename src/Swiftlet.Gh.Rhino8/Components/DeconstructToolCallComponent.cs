using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructToolCallComponent : GH_Component
{
    public DeconstructToolCallComponent()
        : base("Deconstruct Tool Call", "DeCall", "Extracts data from an incoming MCP tool call.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new McpToolCallRequestParam(), "Request", "R", "The tool call request from MCP Server", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpToolCallRequestParam(), "Request", "R", "Pass-through request for MCP Tool Response", GH_ParamAccess.item);
        pManager.AddTextParameter("Tool", "T", "Tool name that was called", GH_ParamAccess.item);
        pManager.AddParameter(new JsonObjectParam(), "Arguments", "A", "The arguments as a JSON object", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        McpToolCallRequestGoo? requestGoo = null;
        if (!DA.GetData(0, ref requestGoo))
        {
            return;
        }

        if (requestGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid request provided");
            return;
        }

        (ModernMcpToolCallContext request, string toolName, System.Text.Json.Nodes.JsonObject arguments) =
            ModernMcpResponseWorkflow.Deconstruct(requestGoo.Value);

        DA.SetData(0, new McpToolCallRequestGoo(request));
        DA.SetData(1, toolName);
        DA.SetData(2, new JsonObjectGoo(arguments));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("A7B8C9D0-E1F2-3456-0123-567890123456");
}

