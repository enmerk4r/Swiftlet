namespace Swiftlet.HostAbstractions;

public interface IHostServices
{
    HostCapabilities Capabilities { get; }

    IBrowserLauncher BrowserLauncher { get; }

    IClipboardService ClipboardService { get; }

    ILocalHttpCallbackListenerFactory LocalCallbacks { get; }

    INotificationSink Notifications { get; }
}
