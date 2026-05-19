namespace Swiftlet.Core.Auth;

public sealed class OAuthAuthorizationSession
{
    public OAuthAuthorizationSession(
        string authorizationUrl,
        string clientId,
        string redirectUri,
        IEnumerable<string>? scopes)
    {
        RedirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
        CodeVerifier = OAuthPkce.GenerateCodeVerifier();
        State = OAuthPkce.GenerateState();

        AuthorizationRequest = new OAuthAuthorizationRequest(
            authorizationUrl,
            clientId,
            redirectUri,
            scopes,
            State,
            OAuthPkce.GenerateCodeChallenge(CodeVerifier));
    }

    public OAuthAuthorizationRequest AuthorizationRequest { get; }

    public string CodeVerifier { get; }

    public string State { get; }

    public string RedirectUri { get; }

    public string AuthorizationUrl => AuthorizationRequest.BuildUrl();
}
