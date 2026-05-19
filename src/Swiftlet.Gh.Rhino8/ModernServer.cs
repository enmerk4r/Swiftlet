using System.Collections.Concurrent;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernServer
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ModernServerRequestContext>> _pendingRequests =
        new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private IReadOnlyList<string> _routes = ["/"];

    public ModernServer(IEnumerable<string>? routes = null)
    {
        ConfigureRoutes(routes);
    }

    public event EventHandler? RequestQueued;

    public IReadOnlyList<string> Routes
    {
        get
        {
            lock (_sync)
            {
                return _routes;
            }
        }
    }

    public void ConfigureRoutes(IEnumerable<string>? routes)
    {
        lock (_sync)
        {
            _routes = ModernServerRouteMatcher.NormalizeRoutes(routes);

            foreach (string route in _pendingRequests.Keys)
            {
                if (!_routes.Contains(route, StringComparer.Ordinal))
                {
                    _pendingRequests.TryRemove(route, out _);
                }
            }
        }
    }

    public bool TryDequeuePendingRequest(string route, out ModernServerRequestContext? context)
    {
        context = null;
        string normalizedRoute = ModernServerRouteMatcher.NormalizeRoute(route);

        if (!_pendingRequests.TryGetValue(normalizedRoute, out ConcurrentQueue<ModernServerRequestContext>? queue))
        {
            return false;
        }

        return queue.TryDequeue(out context);
    }

    public async Task<ModernServerHttpResponse> HandleHttpRequestAsync(
        string httpMethod,
        string path,
        IEnumerable<HttpHeader>? headers,
        IEnumerable<QueryParameter>? queryParameters,
        IRequestBody body,
        CancellationToken cancellationToken = default)
    {
        string[] routes;
        lock (_sync)
        {
            routes = _routes.ToArray();
        }

        string? matchedRoute = ModernServerRouteMatcher.FindBestMatch(path, routes);
        if (matchedRoute is null)
        {
            return ModernServerHttpResponse.PlainText(404, "404 - Route not found");
        }

        var requestContext = new ModernServerRequestContext(
            matchedRoute,
            string.IsNullOrWhiteSpace(path) ? "/" : path,
            httpMethod,
            headers,
            queryParameters,
            body);

        ConcurrentQueue<ModernServerRequestContext> queue = _pendingRequests.GetOrAdd(
            matchedRoute,
            static _ => new ConcurrentQueue<ModernServerRequestContext>());
        queue.Enqueue(requestContext);
        RequestQueued?.Invoke(this, EventArgs.Empty);

        try
        {
            return await requestContext.ResponseTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ModernServerHttpResponse.PlainText(408, "Request timed out waiting for a Grasshopper response.");
        }
    }
}
