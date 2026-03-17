using System.Diagnostics;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8;

public sealed class RhinoBrowserLauncher : IBrowserLauncher
{
    public Task<HostActionResult> OpenUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(url));
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });

            return Task.FromResult(HostActionResult.Success($"Opened browser for {url}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HostActionResult.Manual(
                $"Failed to open browser automatically: {ex.Message}",
                $"Open this URL manually: {url}"));
        }
    }
}
