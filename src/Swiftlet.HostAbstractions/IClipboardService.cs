namespace Swiftlet.HostAbstractions;

public interface IClipboardService
{
    Task<HostActionResult> SetTextAsync(string text, CancellationToken cancellationToken = default);
}
