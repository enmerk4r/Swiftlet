using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateHttpHeaderComponent : GH_Component
{
    public CreateHttpHeaderComponent()
        : base("Create Http Header", "CH", "Create a new Http Header", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.septenary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "Header Key", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "Header Value", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Http Header", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string key = string.Empty;
        string value = string.Empty;

        DA.GetData(0, ref key);
        DA.GetData(1, ref value);

        DA.SetData(0, new HttpHeaderGoo(key, value));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("7DC20210-C08F-466F-AF5E-286F70F4C630");
}

