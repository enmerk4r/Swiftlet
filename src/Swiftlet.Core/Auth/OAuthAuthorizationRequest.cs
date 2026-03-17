namespace Swiftlet.Core.Auth;

public sealed class OAuthAuthorizationRequest
{
    public OAuthAuthorizationRequest(
        string authorizationUrl,
        string clientId,
        string redirectUri,
        IEnumerable<string>? scopes,
        string state,
        string codeChallenge)
    {
        AuthorizationUrl = authorizationUrl ?? throw new ArgumentNullException(nameof(authorizationUrl));
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        RedirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
        Scopes = scopes?.ToArray() ?? [];
        State = state ?? throw new ArgumentNullException(nameof(state));
        CodeChallenge = codeChallenge ?? throw new ArgumentNullException(nameof(codeChallenge));
    }

    public string AuthorizationUrl { get; }

    public string ClientId { get; }

    public string RedirectUri { get; }

    public IReadOnlyList<string> Scopes { get; }

    public string State { get; }

    public string CodeChallenge { get; }

    public string BuildUrl()
    {
        List<string> queryParameters =
        [
            "response_type=code",
            $"client_id={Uri.EscapeDataString(ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(RedirectUri)}",
            $"state={Uri.EscapeDataString(State)}",
            $"code_challenge={Uri.EscapeDataString(CodeChallenge)}",
            "code_challenge_method=S256",
        ];

        string scope = string.Join(" ", Scopes.Where(static scopeValue => !string.IsNullOrWhiteSpace(scopeValue)));
        if (!string.IsNullOrWhiteSpace(scope))
        {
            queryParameters.Add($"scope={Uri.EscapeDataString(scope)}");
        }

        string separator = AuthorizationUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return AuthorizationUrl + separator + string.Join("&", queryParameters);
    }
}
