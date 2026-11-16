using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.OAuthToken)]
public sealed class OAuthTokenComponent : OAuthComponentBase
{
    private readonly OAuthTokenSession _session = new();
    public OAuthTokenComponent() : base(Palette.Entry(nameof(OAuthTokenComponent), "OAuth Token","Exchanges an OAuth 2.0 authorization code for access and refresh tokens.\nSet Refresh to true to use a refresh token to obtain new tokens.", "Swiftlet", "1. Auth")) { }
    public OAuthTokenComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Token URL", "U", "Token endpoint URL (e.g., https://oauth2.googleapis.com/token)");
        inputs.AddText("Client ID", "ID", "OAuth Client ID");
        inputs.AddText("Code", "C", "Authorization code from OAuth Authorize component");
        inputs.AddText("Code Verifier", "V", "PKCE code verifier from OAuth Authorize component").Requirement = Requirement.MayBeMissing;
        inputs.AddText("Redirect URI", "R", "Redirect URI (must match the one used in authorization)");
        inputs.AddText("Client Secret", "S", "Client secret (optional - not needed for PKCE public clients)").Requirement = Requirement.MayBeMissing;
        inputs.AddBoolean("Refresh", "Rf", "Set to true to refresh the token using the Refresh Token output. Use momentarily (e.g., with a Button), not persistently.").Set(false);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Access Token", "AT", "Access token for API requests");
        outputs.AddText("Refresh Token", "RT", "Refresh token for obtaining new access tokens");
        outputs.AddInteger("Expires In", "E", "Token expiration time in seconds");
        outputs.AddText("Token Type", "T", "Token type (usually 'Bearer')");
        outputs.Add(new HttpHeaderParameter("Auth Header", "H", "Ready-to-use Authorization header"));
        outputs.AddText("Error", "Err", "Error message if token exchange failed");
    }
    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string url); access.GetItem(1, out string id); access.GetItem(2, out string code);
        access.GetItem(3, out string verifier); access.GetItem(4, out string redirect);
        access.GetItem(5, out string secret); access.GetItem(6, out bool refresh);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Solution.Token, LifetimeToken);
        var result = _session.EvaluateAsync(new(url ?? string.Empty, id ?? string.Empty, code ?? string.Empty,
            verifier ?? string.Empty, redirect ?? string.Empty, secret ?? string.Empty, refresh), linked.Token).GetAwaiter().GetResult();
        linked.Token.ThrowIfCancellationRequested();
        access.SetItem(0, result.AccessToken); access.SetItem(1, result.RefreshToken);
        if (result.ExpiresIn is int expires) access.SetItem(2, expires);
        access.SetItem(3, result.TokenType); access.SetItem(4, result.Header); access.SetItem(5, result.Error);
        if (result.Diagnostic is not null)
        {
            if (result.IsWarning) access.AddWarning("OAuth", result.Diagnostic);
            else access.AddError("OAuth", result.Diagnostic);
        }
    }
}
