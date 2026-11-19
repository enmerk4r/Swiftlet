using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Rhino;

public sealed class RhinoNotificationSink : INotificationSink
{
    public void Notify(HostNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        string prefix = notification.Severity switch
        {
            HostNotificationSeverity.Error => "[Swiftlet][Error]",
            HostNotificationSeverity.Warning => "[Swiftlet][Warning]",
            _ => "[Swiftlet]",
        };

        global::Rhino.RhinoApp.WriteLine($"{prefix} {notification.Message}");
    }
}
