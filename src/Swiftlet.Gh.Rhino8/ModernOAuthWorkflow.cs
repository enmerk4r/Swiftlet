using Swiftlet.Core.Auth;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8;

// This adapter keeps the Rhino 8 shell aligned with the extracted OAuth core.
// The eventual Grasshopper components should delegate to these helpers rather
// than duplicating PKCE, URL generation, and token request construction.
public static class ModernOAuthWorkflow
{
    public static OAuthAuthorizationSession CreateAuthorizationSession(
        string authorizationUrl,
        string clientId,
        string redirectUri,
        IEnumerable<string>? scopes)
    {
        return new OAuthAuthorizationSession(authorizationUrl, clientId, redirectUri, scopes);
    }

    public static OAuthTokenRequest CreateAuthorizationCodeTokenRequest(
        string tokenUrl,
        string clientId,
        string code,
        string redirectUri,
        string? codeVerifier = null,
        string? clientSecret = null)
    {
        return OAuthTokenRequest.ForAuthorizationCode(tokenUrl, clientId, code, redirectUri, codeVerifier, clientSecret);
    }

    public static OAuthTokenRequest CreateRefreshTokenRequest(
        string tokenUrl,
        string clientId,
        string refreshToken,
        string? clientSecret = null)
    {
        return OAuthTokenRequest.ForRefreshToken(tokenUrl, clientId, refreshToken, clientSecret);
    }

    public static async Task<HostActionResult> LaunchAuthorizationUrlAsync(
        IHostServices hostServices,
        OAuthAuthorizationSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hostServices);
        ArgumentNullException.ThrowIfNull(session);

        HostActionResult result = await hostServices.BrowserLauncher
            .OpenUrlAsync(session.AuthorizationUrl, cancellationToken)
            .ConfigureAwait(false);

        if (result.RequiresManualAction)
        {
            string notificationMessage = string.IsNullOrWhiteSpace(result.ManualActionText)
                ? result.Message
                : $"{result.Message} {result.ManualActionText}";
            hostServices.Notifications.Notify(new HostNotification(
                HostNotificationSeverity.Warning,
                notificationMessage));
        }
        else if (!result.IsSuccess)
        {
            hostServices.Notifications.Notify(new HostNotification(
                HostNotificationSeverity.Error,
                result.Message));
        }

        return result;
    }

    public static async Task<OAuthCallbackResult> WaitForAuthorizationCodeAsync(
        ILocalHttpCallbackSession callbackSession,
        OAuthAuthorizationSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callbackSession);
        ArgumentNullException.ThrowIfNull(session);

        LocalHttpCallbackRequest request = await callbackSession
            .WaitForCallbackAsync(cancellationToken)
            .ConfigureAwait(false);

        OAuthCallbackResult result = CreateCallbackResult(request, session.State);

        await callbackSession.SendResponseAsync(BuildResponse(result), cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static OAuthCallbackResult CreateCallbackResult(LocalHttpCallbackRequest request, string expectedState)
    {
        string? code = request.GetQueryParameter("code");
        string? state = request.GetQueryParameter("state");
        string? error = request.GetQueryParameter("error");
        string? errorDescription = request.GetQueryParameter("error_description");

        if (!string.IsNullOrWhiteSpace(state) &&
            !string.Equals(state, expectedState, StringComparison.Ordinal))
        {
            return OAuthCallbackResult.Failure("State mismatch - possible CSRF attack");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            string message = string.IsNullOrWhiteSpace(errorDescription)
                ? error
                : $"{error}: {errorDescription}";

            return OAuthCallbackResult.Failure(message);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return OAuthCallbackResult.Failure("No authorization code received");
        }

        return OAuthCallbackResult.Success(code, state);
    }

    private static LocalHttpCallbackResponse BuildResponse(OAuthCallbackResult result)
    {
        if (result.IsSuccess)
        {
            return LocalHttpCallbackResponse.Html(
                200,
                BuildHtmlPage("Authorization Successful", "You can close this window and return to Grasshopper."));
        }

        return LocalHttpCallbackResponse.Html(
            200,
            BuildHtmlPage("Authorization Failed", result.Error ?? "The OAuth callback failed."));
    }

    private static string BuildHtmlPage(string title, string message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>{title}</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #dfe8ff 0%, #f7f4ea 100%);
            color: #1f2937;
        }}
        .panel {{
            max-width: 440px;
            padding: 32px;
            border-radius: 18px;
            background: rgba(255, 255, 255, 0.92);
            box-shadow: 0 18px 48px rgba(15, 23, 42, 0.18);
            text-align: center;
        }}
        h1 {{
            margin: 0 0 12px;
            font-size: 28px;
        }}
        p {{
            margin: 0;
            line-height: 1.5;
        }}
    </style>
</head>
<body>
    <div class=""panel"">
        <h1>{title}</h1>
        <p>{message}</p>
    </div>
</body>
</html>";
    }
}
