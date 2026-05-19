using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swiftlet.Gh.Rhino8;

internal sealed class BridgeProcessDiagnostics
{
    private readonly ConcurrentQueue<string> _stderrLines = new();
    private readonly int _maxLines;

    public BridgeProcessDiagnostics(int maxLines = 12)
    {
        _maxLines = Math.Max(1, maxLines);
    }

    public void RecordStderr(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _stderrLines.Enqueue(line.Trim());
        while (_stderrLines.Count > _maxLines && _stderrLines.TryDequeue(out _))
        {
        }
    }

    public InvalidOperationException CreateStartupException(Process process, string defaultMessage)
    {
        string stderr = string.Join(" | ", _stderrLines);
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            return new InvalidOperationException($"{defaultMessage}: {stderr}");
        }

        if (process.HasExited)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && process.ExitCode == 137)
            {
                return new InvalidOperationException(
                    $"{defaultMessage}. SwiftletBridge exited with code 137. macOS likely killed the bridge process; this is commonly caused by a quarantined or unsigned bridge binary. Try running: xattr -dr com.apple.quarantine \"<Swiftlet package>/bridge\" && codesign --force --sign - \"<Swiftlet package>/bridge/osx-arm64/SwiftletBridge\"");
            }

            return new InvalidOperationException($"{defaultMessage}. SwiftletBridge exited with code {process.ExitCode}.");
        }

        return new InvalidOperationException(defaultMessage);
    }
}
