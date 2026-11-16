using Grasshopper.Kernel;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class OAuthAuthorizeComponent : GH_Component
{
    private readonly OAuthAuthorizationController _controller;
    private long _generation;
    private int _updateScheduled;

    public OAuthAuthorizeComponent() : this(new RhinoHostServices()) { }

    public OAuthAuthorizeComponent(IHostServices host)
        : base(
            "OAuth Authorize",
            "OAuth",
            "Initiates OAuth 2.0 Authorization Code flow with PKCE.\nOpens browser for user authentication and receives the authorization code via localhost callback.",
            ShellNaming.Category,
            ShellNaming.Auth)
    {
        _controller = new OAuthAuthorizationController(host);
        _controller.Changed += OnChanged;
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Auth URL", "U", "Authorization endpoint URL (e.g., https://accounts.google.com/o/oauth2/v2/auth)", GH_ParamAccess.item);
        pManager.AddTextParameter("Client ID", "ID", "OAuth Client ID from your app registration", GH_ParamAccess.item);
        pManager.AddTextParameter("Scopes", "S", "OAuth scopes to request (space-separated or as list)", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Port", "P", "Localhost port for callback (default: 8888)", GH_ParamAccess.item, 8888);
        pManager.AddBooleanParameter("Authorize", "A", "Set to true to start authorization flow", GH_ParamAccess.item, false);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Code", "C", "Authorization code (use with OAuth Token component)", GH_ParamAccess.item);
        pManager.AddTextParameter("Code Verifier", "V", "PKCE code verifier (use with OAuth Token component)", GH_ParamAccess.item);
        pManager.AddTextParameter("Redirect URI", "R", "Redirect URI used (use with OAuth Token component)", GH_ParamAccess.item);
        pManager.AddTextParameter("Status", "St", "Current status or error message", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string authorizationUrl = string.Empty;
        string clientId = string.Empty;
        List<string> scopes = [];
        int port = 8888;
        bool authorize = false;

        if (!DA.GetData(0, ref authorizationUrl) || !DA.GetData(1, ref clientId))
        {
            return;
        }

        DA.GetDataList(2, scopes);
        DA.GetData(3, ref port);
        DA.GetData(4, ref authorize);
        var value = _controller.EvaluateAsync(authorizationUrl, clientId, scopes, port, authorize).GetAwaiter().GetResult();
        DA.SetData(0, value.Code); DA.SetData(1, value.Verifier); DA.SetData(2, value.RedirectUri); DA.SetData(3, value.Status);
        if (value.Error is not null) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, value.Error);
        if (value.Warning is not null) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, value.Warning);
        Message = value.IsWaiting ? "Waiting..." : value.Code is not null ? "Authorized" : value.Error is not null ? "Error" : string.Empty;
    }
    private void OnChanged(object? sender, EventArgs args)
    {
        long generation = Volatile.Read(ref _generation);
        if (Interlocked.Exchange(ref _updateScheduled, 1) != 0) return;
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            if (generation != Volatile.Read(ref _generation)) return;
            Interlocked.Exchange(ref _updateScheduled, 0);
            OnPingDocument()?.ScheduleSolution(5, _ => ExpireSolution(false));
        }));
    }
    private void Stop()
    {
        Interlocked.Increment(ref _generation); Interlocked.Exchange(ref _updateScheduled, 0);
        _controller.ResetAsync().GetAwaiter().GetResult();
    }
    public override void RemovedFromDocument(GH_Document document)
    { Stop(); base.RemovedFromDocument(document); }
    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    { if (context == GH_DocumentContext.Close) Stop(); base.DocumentContextChanged(document, context); }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B2C3D4E5-F6A7-4B8C-9D0E-1F2A3B4C5D6E");

}
