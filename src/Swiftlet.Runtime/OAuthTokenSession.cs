using Swiftlet.Core.Auth;
using Swiftlet.Core.Http;

namespace Swiftlet.Runtime;

public sealed record OAuthTokenInputs(string TokenUrl, string ClientId, string Code, string CodeVerifier,
    string RedirectUri, string ClientSecret, bool Refresh);

public sealed record OAuthTokenSnapshot(string? AccessToken = null, string? RefreshToken = null,
    int? ExpiresIn = null, string? TokenType = null, string? Error = null, string Status = "",
    string? Diagnostic = null, bool IsWarning = false)
{
    public HttpHeader? Header => string.IsNullOrWhiteSpace(AccessToken) ? null : new("Authorization", $"{TokenType} {AccessToken}");
}

/// <summary>Serializes token exchange, duplicate suppression and refresh edges for both shells.
/// Tokens are transient and deliberately never serialized into component archives.</summary>
public sealed class OAuthTokenSession
{
    private readonly OAuthTokenClient _client;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private OAuthTokenSnapshot _cached = new(string.Empty, string.Empty, 0, "Bearer");
    private string? _completedKey;
    private string? _failedKey;
    private string? _lastError;
    private bool _previousRefresh;

    public OAuthTokenSession(OAuthTokenClient? client = null) => _client = client ?? new OAuthTokenClient();
    public OAuthTokenSnapshot Cached => Volatile.Read(ref _cached);

    public async Task<OAuthTokenSnapshot> EvaluateAsync(OAuthTokenInputs input, CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        bool priorRefresh = _previousRefresh;
        try
        {
            token.ThrowIfCancellationRequested();
            bool refreshTriggered = input.Refresh && !_previousRefresh;
            _previousRefresh = input.Refresh;
            if (string.IsNullOrWhiteSpace(input.TokenUrl)) return Invalid("Token URL is required");
            if (string.IsNullOrWhiteSpace(input.ClientId)) return Invalid("Client ID is required");
            OAuthTokenRequest request;
            string key;
            if (input.Refresh)
            {
                if (string.IsNullOrWhiteSpace(_cached.RefreshToken))
                    return new(Error: "No refresh token available", Status: "Error", Diagnostic: "No refresh token available. Obtain tokens first with Refresh set to False.");
                if (!refreshTriggered) return _cached with { Status = "Ready" };
                key = string.Join("\n", "refresh_token", input.TokenUrl.Trim(), input.ClientId.Trim(), input.ClientSecret.Trim(), _cached.RefreshToken.Trim());
                request = ModernOAuthWorkflow.CreateRefreshTokenRequest(input.TokenUrl, input.ClientId, _cached.RefreshToken, input.ClientSecret);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(input.Code))
                    return !string.IsNullOrWhiteSpace(_cached.AccessToken) ? _cached with { Status = "Ready" }
                        : new(Error: "Waiting for authorization code", Status: "Waiting", Diagnostic: "Authorization code is required", IsWarning: true);
                if (string.IsNullOrWhiteSpace(input.RedirectUri)) return Invalid("Redirect URI is required");
                key = string.Join("\n", "authorization_code", input.TokenUrl.Trim(), input.ClientId.Trim(), input.Code.Trim(), input.CodeVerifier.Trim(), input.RedirectUri.Trim(), input.ClientSecret.Trim());
                if (_completedKey == key && !string.IsNullOrWhiteSpace(_cached.AccessToken)) return _cached with { Status = "Token received" };
                if (_failedKey == key) return new(Error: _lastError, Status: "Error");
                request = ModernOAuthWorkflow.CreateAuthorizationCodeTokenRequest(input.TokenUrl, input.ClientId, input.Code, input.RedirectUri,
                    string.IsNullOrWhiteSpace(input.CodeVerifier) ? null : input.CodeVerifier,
                    string.IsNullOrWhiteSpace(input.ClientSecret) ? null : input.ClientSecret);
            }
            try
            {
                var response = await _client.ExchangeAsync(request, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                _cached = new(response.AccessToken, string.IsNullOrWhiteSpace(response.RefreshToken) ? _cached.RefreshToken : response.RefreshToken,
                    response.ExpiresIn, response.TokenType, Status: "Token received");
                // Refresh must not forget which one-use authorization code has already been
                // redeemed, or lowering Refresh would replay that code on the next solve.
                if (!input.Refresh) _completedKey = key;
                _failedKey = null; _lastError = null;
                return _cached;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                _failedKey = key; _lastError = error.Message;
                return new(Error: error.Message, Status: "Error");
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A canceled operation must not consume a refresh edge or publish partial tokens.
            _previousRefresh = priorRefresh;
            throw;
        }
        finally { _gate.Release(); }
    }
    private static OAuthTokenSnapshot Invalid(string error) => new(Error: error, Diagnostic: error);
}
