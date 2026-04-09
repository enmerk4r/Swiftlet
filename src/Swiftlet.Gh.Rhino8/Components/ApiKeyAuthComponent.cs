using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ApiKeyAuthComponent : GH_Component
{
    public ApiKeyAuthComponent()
        : base("API Key", "API", "Create a header and a query param (you'll likely need one or the other, not both) for API key auth", ShellNaming.Category, ShellNaming.Auth)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "Header key for your API auth", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "Your API key value", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Your Auth Key-Value as an Http Header", GH_ParamAccess.item);
        pManager.AddParameter(new QueryParameterParam(), "Query Param", "P", "Your Auth Key-Value as a URL query param", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string key = string.Empty;
        string value = string.Empty;

        DA.GetData(0, ref key);
        DA.GetData(1, ref value);

        DA.SetData(0, new HttpHeaderGoo(key, value));
        DA.SetData(1, new QueryParameterGoo(key, value));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("09B3834F-0211-4339-A88E-738202F228FA");
}

