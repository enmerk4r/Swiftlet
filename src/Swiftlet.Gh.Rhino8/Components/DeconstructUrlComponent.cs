using Grasshopper.Kernel;
using Microsoft.AspNetCore.WebUtilities;
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

        if (!url.StartsWith("http", StringComparison.Ordinal))
        {
            throw new Exception(" A valid URL must include a scheme (http or https)");
        }

        Uri uri = new(url);
        var query = QueryHelpers.ParseQuery(uri.Query);
        List<QueryParameterGoo> parameters = [];
        foreach ((string key, var value) in query)
        {
            parameters.Add(new QueryParameterGoo(key, value.ToString()));
        }

        string baseUri = url.Split('?').First();
        DA.SetData(0, baseUri);
        DA.SetData(1, uri.Scheme);
        DA.SetData(2, uri.Host);
        DA.SetData(3, uri.AbsolutePath);
        DA.SetDataList(4, parameters);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("6240a6fa-f6fa-47af-a796-0a2fc3dd6cc1");
}

