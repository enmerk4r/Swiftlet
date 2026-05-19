using Swiftlet.Core.Auth;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

// This is the first migration foothold for the Rhino 8 shell: it consumes
// Swiftlet.Core request and OAuth types directly so later component ports do
// not have to start from placeholders.
public static class ModernRequestWorkflow
{
    public static HttpRequestDefinition CreateRequest(
        string url,
        string method,
        IRequestBody? body,
        IEnumerable<QueryParameter>? queryParameters,
        IEnumerable<HttpHeader>? headers,
        int timeoutSeconds)
    {
        return new HttpRequestDefinition(url, method, body, queryParameters, headers, timeoutSeconds);
    }

    public static OAuthAuthorizationRequest CreateOAuthAuthorizationRequest(
        string authorizationUrl,
        string clientId,
        string redirectUri,
        IEnumerable<string>? scopes,
        string state,
        string codeChallenge)
    {
        return new OAuthAuthorizationRequest(authorizationUrl, clientId, redirectUri, scopes, state, codeChallenge);
    }
}
