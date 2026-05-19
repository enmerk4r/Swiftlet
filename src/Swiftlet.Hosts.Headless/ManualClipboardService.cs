using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Headless;

public sealed class ManualClipboardService : IClipboardService
{
    public Task<HostActionResult> SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(HostActionResult.Manual(
            "Clipboard access is not available in this host.",
            text));
    }
}
