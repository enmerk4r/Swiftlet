using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructUrlComponent : GH_Component
{
    public DeconstructUrlComponent()
        : base("Deconstruct URL", "DURL", "Deconstruct a URL into its constituent parts", ShellNaming.Category, ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URL", "U", "A URL string to deconstruct", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Base", "B", "Base URL", GH_ParamAccess.item);
        pManager.AddTextParameter("Scheme", "S", "URL scheme (http or https)", GH_ParamAccess.item);
        pManager.AddTextParameter("Host", "H", "Host component of the URL", GH_ParamAccess.item);
        pManager.AddTextParameter("Route", "R", "A route (path) to the online resource", GH_ParamAccess.item);
        pManager.AddParameter(new QueryParameterParam(), "Params", "P", "Http Query Parameters", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string url = string.Empty;
        if (!DA.GetData(0, ref url))
        {
            return;
        }

        var parts = HttpDeconstructionWorkflow.DeconstructUrl(url);
        DA.SetData(0, parts.Base);
        DA.SetData(1, parts.Scheme);
        DA.SetData(2, parts.Host);
        DA.SetData(3, parts.Route);
        DA.SetDataList(4, parts.Parameters.Select(static p => new QueryParameterGoo(p)));

    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("6240a6fa-f6fa-47af-a796-0a2fc3dd6cc1");
}
