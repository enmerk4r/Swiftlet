using Swiftlet.Core.Http;

namespace Swiftlet.Core.Auth;

public sealed class OAuthTokenRequest
{
    private OAuthTokenRequest(
        string tokenUrl,
        OAuthGrantType grantType,
        string clientId,
        string? clientSecret,
        string? code,
        string? codeVerifier,
        string? redirectUri,
        string? refreshToken)
    {
        TokenUrl = tokenUrl ?? throw new ArgumentNullException(nameof(tokenUrl));
        GrantType = grantType;
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        ClientSecret = clientSecret;
        Code = code;
        CodeVerifier = codeVerifier;
        RedirectUri = redirectUri;
        RefreshToken = refreshToken;
    }

    public string TokenUrl { get; }

    public OAuthGrantType GrantType { get; }

    public string ClientId { get; }

    public string? ClientSecret { get; }

    public string? Code { get; }

    public string? CodeVerifier { get; }

    public string? RedirectUri { get; }

    public string? RefreshToken { get; }

    public static OAuthTokenRequest ForAuthorizationCode(
        string tokenUrl,
        string clientId,
        string code,
        string redirectUri,
        string? codeVerifier = null,
        string? clientSecret = null)
    {
        Guard.ThrowIfNullOrWhiteSpace(code, nameof(code));
        Guard.ThrowIfNullOrWhiteSpace(redirectUri, nameof(redirectUri));

        return new OAuthTokenRequest(
            tokenUrl,
            OAuthGrantType.AuthorizationCode,
            clientId,
            clientSecret,
            code,
            codeVerifier,
            redirectUri,
            null);
    }

    public static OAuthTokenRequest ForRefreshToken(
        string tokenUrl,
        string clientId,
        string refreshToken,
        string? clientSecret = null)
    {
        Guard.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));

        return new OAuthTokenRequest(
            tokenUrl,
            OAuthGrantType.RefreshToken,
            clientId,
            clientSecret,
            null,
            null,
            null,
            refreshToken);
    }

    public RequestBodyFormUrlEncoded ToRequestBody()
    {
        List<KeyValuePair<string, string>> parameters =
        [
            new("client_id", ClientId),
        ];

        if (GrantType == OAuthGrantType.RefreshToken)
        {
            parameters.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
            parameters.Add(new KeyValuePair<string, string>("refresh_token", RefreshToken ?? string.Empty));
        }
        else
        {
            parameters.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            parameters.Add(new KeyValuePair<string, string>("code", Code ?? string.Empty));
            parameters.Add(new KeyValuePair<string, string>("redirect_uri", RedirectUri ?? string.Empty));

            if (!string.IsNullOrWhiteSpace(CodeVerifier))
            {
                parameters.Add(new KeyValuePair<string, string>("code_verifier", CodeVerifier));
            }
        }

        if (!string.IsNullOrWhiteSpace(ClientSecret))
        {
            parameters.Add(new KeyValuePair<string, string>("client_secret", ClientSecret));
        }

        return new RequestBodyFormUrlEncoded(parameters);
    }
}
