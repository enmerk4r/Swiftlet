namespace Swiftlet.HostAbstractions;

public interface INotificationSink
{
    void Notify(HostNotification notification);
}
