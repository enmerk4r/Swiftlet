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

        string normalizedUrl = url.Trim();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Launch(normalizedUrl);

            return Task.FromResult(HostActionResult.Success("Browser launch requested."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HostActionResult.Manual(
                $"Automatic browser launch failed: {ex.Message}",
                normalizedUrl));
        }
    }

    private static void Launch(string url)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { url },
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                ArgumentList = { url },
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            return;
        }

        throw new InvalidOperationException("Automatic browser launch is not available for this host.");
    }
}
