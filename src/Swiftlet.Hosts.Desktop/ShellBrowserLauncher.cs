using System.Diagnostics;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Desktop;

public sealed class ShellBrowserLauncher : IBrowserLauncher
{
    public Task<HostActionResult> OpenUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(url));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });

            return Task.FromResult(HostActionResult.Success("Browser launch requested."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HostActionResult.Manual(
                $"Automatic browser launch failed: {ex.Message}",
                url));
        }
    }
}
