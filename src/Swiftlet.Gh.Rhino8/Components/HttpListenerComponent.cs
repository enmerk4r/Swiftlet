using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class HttpListenerComponent : GH_Component
{
    private readonly SimpleHttpListenerSession _session = new();
    private bool _requestTriggeredSolve;

    public HttpListenerComponent()
        : base("HTTP Listener", "L", "A simple HTTP listener component that can receive HTTP requests. \nThis component is VERY ALPHA. Use at your own risk.", ShellNaming.Category, ShellNaming.Server)
    {
        _session.RequestReceived += OnRequestReceived;
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Port", "P", "Port number to listen at between 0 and 65353", GH_ParamAccess.item);
        pManager.AddTextParameter("Route", "R", "Optional route prefix", GH_ParamAccess.item);
        pManager.AddParameter(new RequestBodyParam(), "Response Body", "B", "Pre-canned response body that will be returned to the sender", GH_ParamAccess.item);

        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Method", "M", "Request Method", GH_ParamAccess.item);
        pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Http Request Headers", GH_ParamAccess.list);
        pManager.AddParameter(new QueryParameterParam(), "Query Params", "Q", "Components of the request query string", GH_ParamAccess.list);
        pManager.AddTextParameter("Content", "C", "Request Content", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int port = 80;
        string route = string.Empty;
        RequestBodyGoo? responseBody = null;

        if (!DA.GetData(0, ref port))
        {
            return;
        }

        DA.GetData(1, ref route);
        DA.GetData(2, ref responseBody);

        if (port < 0 || port > 65353)
        {
            throw new Exception("Port number must be between 0 and 65353");
        }

        if (!string.IsNullOrEmpty(route) && !route.EndsWith("/"))
        {
            route += "/";
        }

        string uri = $"http://localhost:{port}/{route}";

        try
        {
            _session.ReconfigureAsync(port, route, responseBody?.Value).GetAwaiter().GetResult();
            Message = uri;
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        if (!_requestTriggeredSolve)
        {
            DA.SetData(0, null);
            DA.SetDataList(1, Array.Empty<HttpHeaderGoo>());
            DA.SetDataList(2, Array.Empty<QueryParameterGoo>());
            DA.SetData(3, null);
            return;
        }

        if (_session.LatestRequest is { } request)
        {
            DA.SetData(0, request.Method);
            DA.SetDataList(1, request.Headers.Select(static header => new HttpHeaderGoo(header)));
            DA.SetDataList(2, request.QueryParameters.Select(static parameter => new QueryParameterGoo(parameter)));
            DA.SetData(3, request.Content);
        }

        _requestTriggeredSolve = false;
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        _session.RequestReceived -= OnRequestReceived;
        _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.DocumentContextChanged(document, context);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("EB521F46-C8EE-49B3-AD30-0EAFB8B2B75E");

    private void OnRequestReceived(object? sender, EventArgs e)
    {
        _requestTriggeredSolve = true;
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() => ExpireSolution(true)));
    }
}
