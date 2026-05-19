using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Desktop;

public sealed class DesktopHostServices : IHostServices
{
    public DesktopHostServices(
        IBrowserLauncher? browserLauncher = null,
        IClipboardService? clipboardService = null,
        ILocalHttpCallbackListenerFactory? localCallbacks = null,
        INotificationSink? notifications = null)
    {
        BrowserLauncher = browserLauncher ?? new ShellBrowserLauncher();
        ClipboardService = clipboardService ?? new CommandClipboardService();
        LocalCallbacks = localCallbacks ?? new LoopbackHttpCallbackListenerFactory();
        Notifications = notifications ?? NullNotificationSink.Instance;
        Capabilities = new HostCapabilities(
            canLaunchBrowser: true,
            canUseClipboard: true,
            canShowDialogs: false,
            canAcceptLocalHttpCallbacks: true);
    }

    public HostCapabilities Capabilities { get; }

    public IBrowserLauncher BrowserLauncher { get; }

    public IClipboardService ClipboardService { get; }

    public ILocalHttpCallbackListenerFactory LocalCallbacks { get; }

    public INotificationSink Notifications { get; }
}
