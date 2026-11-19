using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateUrlComponent : GH_Component
{
    public CreateUrlComponent()
        : base("Create URL", "CURL", "Construct a URL from its constituent parts", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Scheme", "S", "URL scheme (http or https)", GH_ParamAccess.item);
        pManager.AddTextParameter("Host", "H", "Host component of the URL", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Port", "P", "TCP port number", GH_ParamAccess.item);
        pManager.AddTextParameter("Route", "R", "A route (path) to an online resource", GH_ParamAccess.item);

        pManager[2].Optional = true;
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("URL", "U", "Constructed URL string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string scheme = string.Empty;
        string host = string.Empty;
        int port = -1;
        string route = string.Empty;

        DA.GetData(0, ref scheme);
        DA.GetData(1, ref host);
        DA.GetData(2, ref port);
        DA.GetData(3, ref route);

        var builder = new UriBuilder
        {
            Scheme = scheme,
            Host = host,
        };

        if (port > 0)
        {
            builder.Port = port;
        }

        if (!string.IsNullOrEmpty(route))
        {
            builder.Path = route;
        }

        DA.SetData(0, builder.ToString());
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("104E0B1E-9954-475C-8046-A7259A8967F5");
}

