using System.Text.Json;

namespace Swiftlet.Core.Auth;

public sealed class OAuthTokenResponse
{
    public OAuthTokenResponse(
        string accessToken,
        string? refreshToken,
        int expiresIn,
        string tokenType,
        string rawJson)
    {
        AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        RefreshToken = refreshToken;
        ExpiresIn = expiresIn;
        TokenType = string.IsNullOrWhiteSpace(tokenType) ? "Bearer" : tokenType;
        RawJson = rawJson ?? string.Empty;
    }

    public string AccessToken { get; }

    public string? RefreshToken { get; }

    public int ExpiresIn { get; }

    public string TokenType { get; }

    public string RawJson { get; }

    public static OAuthTokenResponse FromJson(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        string accessToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("OAuth token response did not include access_token.");

        string? refreshToken = root.TryGetProperty("refresh_token", out JsonElement refreshElement)
            ? refreshElement.GetString()
            : null;

        int expiresIn = root.TryGetProperty("expires_in", out JsonElement expiresElement) &&
                        expiresElement.TryGetInt32(out int parsedExpiresIn)
            ? parsedExpiresIn
            : 0;

        string tokenType = root.TryGetProperty("token_type", out JsonElement tokenTypeElement)
            ? tokenTypeElement.GetString() ?? "Bearer"
            : "Bearer";

        return new OAuthTokenResponse(accessToken, refreshToken, expiresIn, tokenType, json);
    }
}
