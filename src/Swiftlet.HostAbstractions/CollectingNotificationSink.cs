namespace Swiftlet.HostAbstractions;

public sealed class CollectingNotificationSink : INotificationSink
{
    private readonly List<HostNotification> _notifications = [];

    public IReadOnlyList<HostNotification> Notifications => _notifications;

    public void Notify(HostNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _notifications.Add(notification);
    }
}
