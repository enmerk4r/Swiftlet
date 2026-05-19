using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructQueryParamComponent : GH_Component
{
    public DeconstructQueryParamComponent()
        : base("Deconstruct Query Param", "DQP", "Deconstruct a Query Param into its constituent parts", ShellNaming.Category, ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new QueryParameterParam(), "Param", "P", "Query Param to deconstruct", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "Query Parameter Key", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "Query Parameter Value", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        QueryParameterGoo? parameter = null;
        if (!DA.GetData(0, ref parameter) || parameter?.Value is null)
        {
            return;
        }

        DA.SetData(0, parameter.Value.Key);
        DA.SetData(1, parameter.Value.Value);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("4a7f8985-730a-45da-803d-d5c21ef1ea0e");
}

