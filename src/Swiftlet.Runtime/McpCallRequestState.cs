using Swiftlet.Core.Mcp;
namespace Swiftlet.Runtime;

/// <summary>One unanswered call per tool; canceled/repeated graph solutions retain the same response handle.</summary>
public sealed class McpCallRequestState
{
    private readonly object _sync = new();
    private readonly Dictionary<string, ModernMcpToolCallContext> _active = new(StringComparer.Ordinal);
    public event EventHandler? RequestReady;
    public ModernMcpToolCallContext?[] Capture(IEnumerable<string> names, Func<string, ModernMcpToolCallContext?> dequeue)
    {
        string[] keys = names.ToArray();
        lock (_sync)
        {
            foreach (string old in _active.Keys.Except(keys, StringComparer.Ordinal).ToArray())
            { var call = _active[old]; _active.Remove(old); call.TryRespondWithError(-32000, "Tool was removed."); }
            var result = new ModernMcpToolCallContext?[keys.Length];
            for (int index = 0; index < keys.Length; index++)
            {
                string name = keys[index];
                if (_active.TryGetValue(name, out var call) && call.HasResponded) _active.Remove(name);
                if (!_active.TryGetValue(name, out call))
                {
                    do { call = dequeue(name); } while (call is not null && call.HasResponded);
                    if (call is not null)
                    {
                        _active[name] = call; var watched = call;
                        _ = call.ResponseTask.ContinueWith(_ => OnResponse(name, watched), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                    }
                }
                result[index] = call;
            }
            return result;
        }
    }
    public void Clear()
    {
        ModernMcpToolCallContext[] calls;
        lock (_sync) { calls = _active.Values.ToArray(); _active.Clear(); }
        foreach (var call in calls) call.TryRespondWithError(-32000, "Server stopped.");
    }
    private void OnResponse(string name, ModernMcpToolCallContext call)
    {
        lock (_sync) if (!_active.TryGetValue(name, out var active) || !ReferenceEquals(active, call)) return;
        RequestReady?.Invoke(this, EventArgs.Empty);
    }
}
