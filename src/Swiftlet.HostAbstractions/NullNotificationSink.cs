namespace Swiftlet.HostAbstractions;

public sealed class NullNotificationSink : INotificationSink
{
    public static NullNotificationSink Instance { get; } = new();

    private NullNotificationSink()
    {
    }

    public void Notify(HostNotification notification)
    {
        _ = notification;
    }
}
