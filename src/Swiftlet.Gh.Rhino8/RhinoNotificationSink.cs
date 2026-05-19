using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8;

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

        Rhino.RhinoApp.WriteLine($"{prefix} {notification.Message}");
    }
}
