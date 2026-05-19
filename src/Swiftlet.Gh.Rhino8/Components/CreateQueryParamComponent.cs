using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateQueryParamComponent : GH_Component
{
    public CreateQueryParamComponent()
        : base("Create Query Param", "CQP", "Create an Http Query Param", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.septenary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "Query Parameter Key", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "Query Parameter Value", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new QueryParameterParam(), "Param", "P", "Http Query Parameter", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string key = string.Empty;
        string value = string.Empty;

        DA.GetData(0, ref key);
        DA.GetData(1, ref value);

        DA.SetData(0, new QueryParameterGoo(key, value));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("BA9BE0B0-AECC-49D4-95C0-491C00CABECB");
}

