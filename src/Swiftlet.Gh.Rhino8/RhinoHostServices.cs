using Swiftlet.HostAbstractions;
using Swiftlet.Hosts.Desktop;
using Swiftlet.Hosts.Headless;

namespace Swiftlet.Gh.Rhino8;

public sealed class RhinoHostServices : IHostServices
{
    private readonly IHostServices _inner;

    public RhinoHostServices(INotificationSink? notifications = null)
    {
        INotificationSink sink = notifications ?? new RhinoNotificationSink();
        _inner = OperatingSystem.IsLinux()
            ? new HeadlessHostServices(sink)
            : new DesktopHostServices(
                browserLauncher: new RhinoBrowserLauncher(),
                clipboardService: new RhinoClipboardService(),
                localCallbacks: new LoopbackHttpCallbackListenerFactory(),
                notifications: sink);
    }

    public HostCapabilities Capabilities => _inner.Capabilities;

    public IBrowserLauncher BrowserLauncher => _inner.BrowserLauncher;

    public IClipboardService ClipboardService => _inner.ClipboardService;

    public ILocalHttpCallbackListenerFactory LocalCallbacks => _inner.LocalCallbacks;

    public INotificationSink Notifications => _inner.Notifications;
}
