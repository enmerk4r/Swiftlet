using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.Doc;
using GrasshopperIO;
using Swiftlet.HostAbstractions;
using Swiftlet.Hosts.Rhino;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.OAuthAuthorize)]
public sealed class OAuthAuthorizeComponent : OAuthComponentBase
{
    private readonly OAuthAuthorizationController _controller;
    public OAuthAuthorizeComponent() : this(new RhinoHostServices()) { }
    public OAuthAuthorizeComponent(IHostServices host) : base(Palette.Entry(nameof(OAuthAuthorizeComponent), "OAuth Authorize","Initiates OAuth 2.0 Authorization Code flow with PKCE.\nOpens browser for user authentication and receives the authorization code via localhost callback.", "Swiftlet", "1. Auth"))
    { _controller = new(host); _controller.Changed += OnChanged; }
    public OAuthAuthorizeComponent(IReader reader) : base(reader)
    { _controller = new(new RhinoHostServices()); _controller.Changed += OnChanged; }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Auth URL", "U", "Authorization endpoint URL (e.g., https://accounts.google.com/o/oauth2/v2/auth)");
        inputs.AddText("Client ID", "ID", "OAuth Client ID from your app registration");
        inputs.AddText("Scopes", "S", "OAuth scopes to request (space-separated or as list)", Access.Twig, Requirement.MayBeMissing);
        inputs.AddInteger("Port", "P", "Localhost port for callback (default: 8888)").Set(8888);
        inputs.AddBoolean("Authorize", "A", "Set to true to start authorization flow").Set(false);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Code", "C", "Authorization code (use with OAuth Token component)");
        outputs.AddText("Code Verifier", "V", "PKCE code verifier (use with OAuth Token component)");
        outputs.AddText("Redirect URI", "R", "Redirect URI used (use with OAuth Token component)");
        outputs.AddText("Status", "St", "Current status or error message");
    }
    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string url); access.GetItem(1, out string client);
        access.GetTwig<string>(2, out var scopes); access.GetItem(3, out int port); access.GetItem(4, out bool authorize);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Solution.Token, LifetimeToken);
        var value = _controller.EvaluateAsync(url, client, scopes?.NonNullItems.ToArray() ?? Array.Empty<string>(), port, authorize, linked.Token).GetAwaiter().GetResult();
        linked.Token.ThrowIfCancellationRequested();
        access.SetItem(0, value.Code); access.SetItem(1, value.Verifier); access.SetItem(2, value.RedirectUri); access.SetItem(3, value.Status);
        if (value.ReportError && value.Error is not null) access.AddError("OAuth", value.Error);
        if (value.Warning is not null) access.AddWarning("OAuth", value.Warning);
        ComponentMessage.Show(this, value.IsWaiting ? "Waiting..." : value.Code is not null ? "Authorized" : value.Error is not null ? "Error" : null, label => Label = label);
    }
    private void OnChanged(object? sender, EventArgs args)
    {
        if (args is not OAuthAuthorizationChangedEventArgs change || change.Generation != _controller.Generation) return;
        var lifetime = LifetimeToken;
        if (lifetime.IsCancellationRequested) return;
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            if (change.Generation != _controller.Generation || lifetime.IsCancellationRequested || Document is not { State: DocumentState.Active } document || Activity == ObjectActivity.Disabled) return;
            document.Solution.DelayedExpire(this);
        }));
    }
    protected override void OnStopped()
    { _controller.ResetAsync().GetAwaiter().GetResult(); }
}
