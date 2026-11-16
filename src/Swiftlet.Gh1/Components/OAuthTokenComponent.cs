using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class OAuthTokenComponent : GH_Component
{
    private OAuthTokenSession _session = new();
    private CancellationTokenSource _lifetime = new();
    private Task<OAuthTokenSnapshot>? _pending;
    private long _generation;
    private int _updateScheduled;

    public OAuthTokenComponent()
        : base(
            "OAuth Token",
            "Token",
            "Exchanges an OAuth 2.0 authorization code for access and refresh tokens.\nSet Refresh to true to use a refresh token to obtain new tokens.",
            ShellNaming.Category,
            ShellNaming.Auth)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Token URL", "U", "Token endpoint URL (e.g., https://oauth2.googleapis.com/token)", GH_ParamAccess.item);
        pManager.AddTextParameter("Client ID", "ID", "OAuth Client ID", GH_ParamAccess.item);
        pManager.AddTextParameter("Code", "C", "Authorization code from OAuth Authorize component", GH_ParamAccess.item);
        pManager.AddTextParameter("Code Verifier", "V", "PKCE code verifier from OAuth Authorize component", GH_ParamAccess.item);
        pManager.AddTextParameter("Redirect URI", "R", "Redirect URI (must match the one used in authorization)", GH_ParamAccess.item);
        pManager.AddTextParameter("Client Secret", "S", "Client secret (optional - not needed for PKCE public clients)", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Refresh", "Rf", "Set to true to refresh the token using the Refresh Token output. Use momentarily (e.g., with a Button), not persistently.", GH_ParamAccess.item, false);

        pManager[3].Optional = true;
        pManager[5].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Access Token", "AT", "Access token for API requests", GH_ParamAccess.item);
        pManager.AddTextParameter("Refresh Token", "RT", "Refresh token for obtaining new access tokens", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Expires In", "E", "Token expiration time in seconds", GH_ParamAccess.item);
        pManager.AddTextParameter("Token Type", "T", "Token type (usually 'Bearer')", GH_ParamAccess.item);
        pManager.AddParameter(new HttpHeaderParam(), "Auth Header", "H", "Ready-to-use Authorization header", GH_ParamAccess.item);
        pManager.AddTextParameter("Error", "Err", "Error message if token exchange failed", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string tokenUrl = string.Empty;
        string clientId = string.Empty;
        string code = string.Empty;
        string codeVerifier = string.Empty;
        string redirectUri = string.Empty;
        string clientSecret = string.Empty;
        bool refresh = false;

        DA.GetData(0, ref tokenUrl);
        DA.GetData(1, ref clientId);
        DA.GetData(2, ref code);
        DA.GetData(3, ref codeVerifier);
        DA.GetData(4, ref redirectUri);
        DA.GetData(5, ref clientSecret);
        DA.GetData(6, ref refresh);

        if (_lifetime.IsCancellationRequested)
        {
            _lifetime.Dispose(); _lifetime = new(); _session = new(); _pending = null;
        }
        if (refresh) AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
            "Refresh is enabled. Set back to False after refreshing to avoid repeated token requests.");
        if (_pending is { IsCompleted: true }) _pending = null;
        _pending ??= _session.EvaluateAsync(new(tokenUrl, clientId, code, codeVerifier, redirectUri, clientSecret, refresh), _lifetime.Token);
        if (_pending.IsCompletedSuccessfully)
        {
            var result = _pending.Result; _pending = null;
            Output(DA, result);
        }
        else if (_pending.IsCompleted)
        {
            _pending.GetAwaiter().GetResult();
            _pending = null;
        }
        else
        {
            Output(DA, refresh ? _session.Cached with { Status = "Refreshing..." }
                : new OAuthTokenSnapshot(Error: "Requesting token...", Status: "Requesting..."));
            if (Interlocked.Exchange(ref _updateScheduled, 1) == 0)
                _ = NotifyCompletionAsync(_pending, _generation);
        }
    }

    private void Output(IGH_DataAccess access, OAuthTokenSnapshot value)
    {
        access.SetData(0, value.AccessToken); access.SetData(1, value.RefreshToken);
        access.SetData(2, value.ExpiresIn); access.SetData(3, value.TokenType);
        access.SetData(4, value.Header is { } header ? new HttpHeaderGoo(header) : null);
        access.SetData(5, value.Error); Message = value.Status;
        if (value.Diagnostic is not null) AddRuntimeMessage(value.IsWarning ? GH_RuntimeMessageLevel.Warning : GH_RuntimeMessageLevel.Error, value.Diagnostic);
    }
    private async Task NotifyCompletionAsync(Task<OAuthTokenSnapshot> pending, long generation)
    {
        try { await pending.ConfigureAwait(false); }
        catch (Exception) { /* Observed here; the next solve reports a faulted task. */ }
        finally
        {
            if (generation == Volatile.Read(ref _generation))
                Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    if (generation != Volatile.Read(ref _generation)) return;
                    Interlocked.Exchange(ref _updateScheduled, 0);
                    OnPingDocument()?.ScheduleSolution(5, _ => ExpireSolution(false));
                }));
        }
    }
    private void StopExchange()
    {
        Interlocked.Increment(ref _generation); Interlocked.Exchange(ref _updateScheduled, 0);
        _lifetime.Cancel(); _pending = null;
    }
    public override void RemovedFromDocument(GH_Document document)
    { StopExchange(); base.RemovedFromDocument(document); }
    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    { if (context == GH_DocumentContext.Close) StopExchange(); base.DocumentContextChanged(document, context); }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("C3D4E5F6-A7B8-4C9D-0E1F-2A3B4C5D6E7F");

}
