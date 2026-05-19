namespace Swiftlet.HostAbstractions;

public sealed class HostNotification
{
    public HostNotification(HostNotificationSeverity severity, string message)
    {
        Severity = severity;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public HostNotificationSeverity Severity { get; }

    public string Message { get; }
}
