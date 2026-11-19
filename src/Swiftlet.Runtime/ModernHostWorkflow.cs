using Swiftlet.HostAbstractions;

namespace Swiftlet.Runtime;

public static class ModernHostWorkflow
{
    public static bool SupportsInteractiveOAuth(IHostServices hostServices)
    {
        ArgumentNullException.ThrowIfNull(hostServices);
        return hostServices.Capabilities.CanLaunchBrowser && hostServices.Capabilities.CanAcceptLocalHttpCallbacks;
    }

    public static bool SupportsClipboardExport(IHostServices hostServices)
    {
        ArgumentNullException.ThrowIfNull(hostServices);
        return hostServices.Capabilities.CanUseClipboard;
    }
}
