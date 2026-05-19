namespace Swiftlet.HostAbstractions;

public sealed class HostCapabilities
{
    public HostCapabilities(
        bool canLaunchBrowser,
        bool canUseClipboard,
        bool canShowDialogs,
        bool canAcceptLocalHttpCallbacks)
    {
        CanLaunchBrowser = canLaunchBrowser;
        CanUseClipboard = canUseClipboard;
        CanShowDialogs = canShowDialogs;
        CanAcceptLocalHttpCallbacks = canAcceptLocalHttpCallbacks;
    }

    public bool CanLaunchBrowser { get; }

    public bool CanUseClipboard { get; }

    public bool CanShowDialogs { get; }

    public bool CanAcceptLocalHttpCallbacks { get; }
}
