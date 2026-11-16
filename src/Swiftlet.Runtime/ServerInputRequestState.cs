namespace Swiftlet.Runtime;

/// <summary>Retains a claimed request across canceled/repeated host solutions until its one-shot response completes.</summary>
public sealed class ServerInputRequestState
{
    private readonly object _sync = new();
    private readonly Dictionary<string, ModernServerRequestContext> _active = new(StringComparer.Ordinal);
    public event EventHandler? RequestReady;

    public ModernServerRequestContext?[] Capture(IEnumerable<string> routes, Func<string, ModernServerRequestContext?> dequeue)
    {
        string[] normalized = routes.Select(ModernServerRouteMatcher.NormalizeRoute).ToArray();
        lock (_sync)
        {
            foreach (string removed in _active.Keys.Except(normalized, StringComparer.Ordinal).ToArray())
            {
                ModernServerRequestContext old = _active[removed];
                _active.Remove(removed);
                old.TrySetResponse(ModernServerHttpResponse.PlainText(503, "Route was removed."));
            }
            var result = new ModernServerRequestContext?[normalized.Length];
            for (int index = 0; index < normalized.Length; index++)
            {
                string route = normalized[index];
                if (_active.TryGetValue(route, out var context) && context.HasResponded)
                    _active.Remove(route);
                if (!_active.TryGetValue(route, out context))
                {
                    do { context = dequeue(route); } while (context is not null && context.HasResponded);
                    if (context is not null)
                    {
                        _active[route] = context;
                        var watched = context;
                        _ = context.ResponseTask.ContinueWith(_ => OnResponseCompleted(route, watched),
                            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                    }
                }
                result[index] = context;
            }
            return result;
        }
    }

    public void Clear()
    {
        ModernServerRequestContext[] contexts;
        lock (_sync) { contexts = _active.Values.ToArray(); _active.Clear(); }
        foreach (var context in contexts)
            context.TrySetResponse(ModernServerHttpResponse.PlainText(503, "Server stopped."));
    }

    private void OnResponseCompleted(string route, ModernServerRequestContext context)
    {
        lock (_sync)
            if (!_active.TryGetValue(route, out var current) || !ReferenceEquals(current, context)) return;
        RequestReady?.Invoke(this, EventArgs.Empty);
    }
}
