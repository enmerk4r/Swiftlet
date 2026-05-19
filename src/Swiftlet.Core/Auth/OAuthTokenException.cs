using System.Text.Json;

namespace Swiftlet.Core.Auth;

public sealed class OAuthTokenException : Exception
{
    public OAuthTokenException(string message)
        : base(message)
    {
    }

    public static OAuthTokenException FromJson(string json, string fallbackMessage)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            string? error = root.TryGetProperty("error", out JsonElement errorElement)
                ? errorElement.GetString()
                : null;

            string? errorDescription = root.TryGetProperty("error_description", out JsonElement descriptionElement)
                ? descriptionElement.GetString()
                : null;

            string message = string.IsNullOrWhiteSpace(errorDescription)
                ? error ?? fallbackMessage
                : $"{error}: {errorDescription}";

            return new OAuthTokenException(message);
        }
        catch (JsonException)
        {
            return new OAuthTokenException(fallbackMessage);
        }
    }
}
