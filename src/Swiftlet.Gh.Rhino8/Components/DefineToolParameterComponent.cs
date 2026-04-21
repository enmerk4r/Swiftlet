using Grasshopper.Kernel;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DefineToolParameterComponent : GH_Component
{
    public DefineToolParameterComponent()
        : base("Define Tool Parameter", "Param", "Defines one input parameter for an MCP tool. These parameter definitions become the schema for the tool's JSON arguments.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "N", "JSON property name for this argument.", GH_ParamAccess.item);
        pManager.AddTextParameter("Type", "T", "JSON value type for this argument: string, number, integer, boolean, object, or array.", GH_ParamAccess.item, "string");
        pManager.AddTextParameter("Description", "D", "Plain-language explanation of what this argument means.", GH_ParamAccess.item, string.Empty);
        pManager.AddBooleanParameter("Required", "R", "If true, the MCP client is expected to provide this argument.", GH_ParamAccess.item, true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpToolParameterParam(), "Parameter", "P", "Tool-parameter definition to connect to Define Tool.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string name = string.Empty;
        string type = "string";
        string description = string.Empty;
        bool required = true;

        if (!DA.GetData(0, ref name))
        {
            return;
        }

        DA.GetData(1, ref type);
        DA.GetData(2, ref description);
        DA.GetData(3, ref required);

        if (string.IsNullOrWhiteSpace(name))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Parameter name cannot be empty");
            return;
        }

        DA.SetData(0, new McpToolParameterGoo(new McpToolParameter(name, type, description, required)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("F6A7B8C9-D0E1-2345-F012-456789012345");
}

