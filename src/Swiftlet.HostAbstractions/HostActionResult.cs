namespace Swiftlet.HostAbstractions;

public sealed class HostActionResult
{
    private HostActionResult(bool isSuccess, bool requiresManualAction, string message, string? manualActionText)
    {
        IsSuccess = isSuccess;
        RequiresManualAction = requiresManualAction;
        Message = message;
        ManualActionText = manualActionText;
    }

    public bool IsSuccess { get; }

    public bool RequiresManualAction { get; }

    public string Message { get; }

    public string? ManualActionText { get; }

    public static HostActionResult Success(string message) => new(true, false, message, null);

    public static HostActionResult Manual(string message, string manualActionText) => new(false, true, message, manualActionText);

    public static HostActionResult Failure(string message) => new(false, false, message, null);
}
