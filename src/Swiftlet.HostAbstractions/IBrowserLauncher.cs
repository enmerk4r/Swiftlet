namespace Swiftlet.HostAbstractions;

public interface IBrowserLauncher
{
    Task<HostActionResult> OpenUrlAsync(string url, CancellationToken cancellationToken = default);
}
