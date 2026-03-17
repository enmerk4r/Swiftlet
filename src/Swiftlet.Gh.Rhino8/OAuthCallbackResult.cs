namespace Swiftlet.Gh.Rhino8;

public sealed class OAuthCallbackResult
{
    private OAuthCallbackResult(bool isSuccess, string? authorizationCode, string? returnedState, string? error)
    {
        IsSuccess = isSuccess;
        AuthorizationCode = authorizationCode;
        ReturnedState = returnedState;
        Error = error;
    }

    public bool IsSuccess { get; }

    public string? AuthorizationCode { get; }

    public string? ReturnedState { get; }

    public string? Error { get; }

    public static OAuthCallbackResult Success(string authorizationCode, string? returnedState) =>
        new(true, authorizationCode, returnedState, null);

    public static OAuthCallbackResult Failure(string error) =>
        new(false, null, null, error);
}
