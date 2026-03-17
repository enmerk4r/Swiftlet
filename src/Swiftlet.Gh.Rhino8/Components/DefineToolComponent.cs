using Grasshopper.Kernel;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DefineToolComponent : GH_Component
{
    public DefineToolComponent()
        : base("Define Tool", "Tool", "Defines an MCP tool that can be called by AI clients.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "N", "Tool name (e.g., 'compute_area')", GH_ParamAccess.item);
        pManager.AddTextParameter("Description", "D", "Description of what the tool does", GH_ParamAccess.item);
        pManager.AddParameter(new McpToolParameterParam(), "Parameters", "P", "Tool parameter definitions", GH_ParamAccess.list);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpToolDefinitionParam(), "Tool", "T", "The tool definition", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string name = string.Empty;
        string description = string.Empty;
        List<McpToolParameterGoo> parameterGoos = [];

        if (!DA.GetData(0, ref name) || !DA.GetData(1, ref description))
        {
            return;
        }

        DA.GetDataList(2, parameterGoos);

        if (string.IsNullOrWhiteSpace(name))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tool name cannot be empty");
            return;
        }

        List<McpToolParameter> parameters = parameterGoos
            .Where(static goo => goo?.Value is not null)
            .Select(static goo => goo!.Value!)
            .ToList();

        DA.SetData(0, new McpToolDefinitionGoo(new McpToolDefinition(name, description, parameters)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("E5F6A7B8-C9D0-1234-EF01-345678901234");
}

