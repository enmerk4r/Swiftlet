using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Headless;

public sealed class ManualBrowserLauncher : IBrowserLauncher
{
    public Task<HostActionResult> OpenUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(url));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(HostActionResult.Manual(
            "Browser launch is not available in this host.",
            url));
    }
}
