using System.Net.Sockets;

namespace Swiftlet.Runtime;

public static class SendUtilities
{
    public static void Delay(int milliseconds, CancellationToken token = default) =>
        Task.Delay(Math.Abs(milliseconds), token).GetAwaiter().GetResult();

    public sealed record UdpResult(bool Success, int BytesSent, string? Error = null, string? Warning = null, bool IsValidationError = false);
    public static async Task<UdpResult> SendUdpAsync(string host, int port, byte[] data, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(host)) return new(false, 0, "Host cannot be empty", IsValidationError: true);
        if (Uri.TryCreate(host, UriKind.Absolute, out Uri? uri) && !string.IsNullOrWhiteSpace(uri.Scheme))
            return new(false, 0, $"UDP does not use URL schemes. Use '{uri.Host}' instead of '{host}'", IsValidationError: true);
        if (port < 0 || port > 65535) return new(false, 0, "Port must be between 0 and 65535", IsValidationError: true);
        if (data.Length == 0) return new(false, 0, Warning: "No data to send");
        try
        {
            using var client = new UdpClient();
            int sent = await client.SendAsync(data.AsMemory(), host, port, token).ConfigureAwait(false);
            return new(true, sent);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) { return new(false, 0, ex.Message); }
    }
}
