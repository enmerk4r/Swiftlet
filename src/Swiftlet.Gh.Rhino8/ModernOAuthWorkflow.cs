using System.Net;
using System.Text;
using Swiftlet.Core.Auth;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8;

// This adapter keeps the Rhino 8 shell aligned with the extracted OAuth core.
// The eventual Grasshopper components should delegate to these helpers rather
// than duplicating PKCE, URL generation, and token request construction.
public static class ModernOAuthWorkflow
{
    private const string RelayMarkerParameter = "swiftlet_relay";

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

        while (true)
        {
            LocalHttpCallbackRequest request = await callbackSession
                .WaitForCallbackAsync(cancellationToken)
                .ConfigureAwait(false);

            OAuthCallbackResult result = CreateCallbackResult(request, session.State);
            if (ShouldContinueBrowserRelay(request, result))
            {
                await callbackSession.SendResponseAsync(BuildContinuationResponse(), cancellationToken).ConfigureAwait(false);
                continue;
            }

            await callbackSession.SendResponseAsync(BuildResponse(result), cancellationToken).ConfigureAwait(false);
            return result;
        }
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

            return OAuthCallbackResult.Failure(WithCallbackDetails(message, request));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return OAuthCallbackResult.Failure(WithCallbackDetails("No authorization code received", request));
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

    private static bool ShouldContinueBrowserRelay(LocalHttpCallbackRequest request, OAuthCallbackResult result)
    {
        if (result.IsSuccess || !string.Equals(result.Error, "No authorization code received", StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(request.GetQueryParameter(RelayMarkerParameter), "1", StringComparison.Ordinal);
    }

    private static LocalHttpCallbackResponse BuildContinuationResponse()
    {
        return LocalHttpCallbackResponse.Html(200, """
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Completing Authorization</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #dfe8ff 0%, #f7f4ea 100%);
            color: #1f2937;
        }
        .panel {
            max-width: 480px;
            padding: 32px;
            border-radius: 18px;
            background: rgba(255, 255, 255, 0.92);
            box-shadow: 0 18px 48px rgba(15, 23, 42, 0.18);
            text-align: center;
        }
        h1 {
            margin: 0 0 12px;
            font-size: 28px;
        }
        p {
            margin: 0;
            line-height: 1.5;
        }
    </style>
</head>
<body>
    <div class="panel">
        <h1>Completing Authorization</h1>
        <p id="message">Finishing the browser callback...</p>
    </div>
    <script>
        (function () {
            const message = document.getElementById('message');
            const current = new URL(window.location.href);
            const params = new URLSearchParams(current.search);
            const hash = current.hash.startsWith('#') ? current.hash.substring(1) : current.hash;
            const hashParams = new URLSearchParams(hash);
            const alreadyRelayed = params.get('swiftlet_relay') === '1';

            for (const [key, value] of hashParams.entries()) {
                if (!params.has(key)) {
                    params.set(key, value);
                }
            }

            if (alreadyRelayed) {
                message.textContent = 'Authorization callback relay already ran once. Waiting for Swiftlet to finish...';
                return;
            }

            params.set('swiftlet_relay', '1');
            params.set('swiftlet_browser_href', window.location.href);
            params.set('swiftlet_browser_hash', window.location.hash || '');
            params.set('swiftlet_relay_status', [...hashParams.keys()].length === 0 ? 'no_hash_params' : 'relayed_hash_params');

            const form = document.createElement('form');
            form.method = 'post';
            form.action = current.origin + current.pathname;

            for (const [key, value] of params.entries()) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = key;
                input.value = value;
                form.appendChild(input);
            }

            document.body.appendChild(form);
            form.submit();
        })();
    </script>
</body>
</html>
""");
    }
    private static string BuildHtmlPage(string title, string message)
    {
        string encodedTitle = WebUtility.HtmlEncode(title);
        string encodedMessage = WebUtility.HtmlEncode(message);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>{encodedTitle}</title>
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
        <h1>{encodedTitle}</h1>
        <p>{encodedMessage}</p>
    </div>
</body>
</html>";
    }

    private static string WithCallbackDetails(string message, LocalHttpCallbackRequest request)
    {
        return $"{message}. Callback details: {FormatCallbackDetails(request)}";
    }

    private static string FormatCallbackDetails(LocalHttpCallbackRequest request)
    {
        var builder = new StringBuilder();
        builder.Append("method=").Append(request.HttpMethod);
        builder.Append(", url=").Append(request.RequestUri);
        builder.Append(", query=").Append(FormatStringMap(request.QueryParameters));

        if (request.Headers.Count > 0)
        {
            builder.Append(", headers=").Append(FormatHeaderMap(request.Headers));
        }

        if (!string.IsNullOrWhiteSpace(request.RawBody))
        {
            builder.Append(", body=").Append(request.RawBody);
        }

        return builder.ToString();
    }

    private static string FormatStringMap(IReadOnlyDictionary<string, string?> values)
    {
        if (values.Count == 0)
        {
            return "{}";
        }

        return "{" + string.Join(", ", values.Select(static pair => $"{pair.Key}={pair.Value ?? string.Empty}")) + "}";
    }

    private static string FormatHeaderMap(IReadOnlyDictionary<string, string> values)
    {
        if (values.Count == 0)
        {
            return "{}";
        }

        return "{" + string.Join(", ", values.Select(static pair => $"{pair.Key}={pair.Value}")) + "}";
    }
}
