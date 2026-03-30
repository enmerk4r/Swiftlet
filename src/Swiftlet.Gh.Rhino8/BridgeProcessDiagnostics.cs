using System.Collections.Concurrent;
using System.Diagnostics;

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
            return new InvalidOperationException($"{defaultMessage}. SwiftletBridge exited with code {process.ExitCode}.");
        }

        return new InvalidOperationException(defaultMessage);
    }
}
