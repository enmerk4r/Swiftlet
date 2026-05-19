using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Headless;

public sealed class HeadlessHostServices : IHostServices
{
    public HeadlessHostServices(INotificationSink? notifications = null)
    {
        BrowserLauncher = new ManualBrowserLauncher();
        ClipboardService = new ManualClipboardService();
        LocalCallbacks = new UnsupportedLocalHttpCallbackListenerFactory();
        Notifications = notifications ?? NullNotificationSink.Instance;
        Capabilities = new HostCapabilities(
            canLaunchBrowser: false,
            canUseClipboard: false,
            canShowDialogs: false,
            canAcceptLocalHttpCallbacks: false);
    }

    public HostCapabilities Capabilities { get; }

    public IBrowserLauncher BrowserLauncher { get; }

    public IClipboardService ClipboardService { get; }

    public ILocalHttpCallbackListenerFactory LocalCallbacks { get; }

    public INotificationSink Notifications { get; }
}
