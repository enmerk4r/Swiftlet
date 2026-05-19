namespace Swiftlet.Core.Http;

public sealed class UrlValidationResult
{
    private UrlValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public bool IsValid { get; }

    public string? ErrorMessage { get; }

    public static UrlValidationResult Success() => new(true, null);

    public static UrlValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
