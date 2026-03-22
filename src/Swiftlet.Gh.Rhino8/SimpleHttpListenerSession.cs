using System.Net;
using System.Text;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

public sealed class SimpleHttpListenerSession : IAsyncDisposable
{
    private readonly object _sync = new();
    private HttpListener? _listener;
    private Task? _listenTask;
    private string _route = "/";
    private IRequestBody? _responseBody;

    public event EventHandler? RequestReceived;

    public int Port { get; private set; }

    public string Route
    {
        get
        {
            lock (_sync)
            {
                return _route;
            }
        }
    }

    public SimpleHttpListenerRequest? LatestRequest { get; private set; }

    public bool IsRunning => _listener?.IsListening == true;

    public string StatusMessage
    {
        get
        {
            if (!IsRunning || Port <= 0)
            {
                return "Stopped";
            }

            string route = Route;
            return route == "/"
                ? $"http://localhost:{Port}/"
                : $"http://localhost:{Port}{route}/";
        }
    }

    public async Task ReconfigureAsync(
        int port,
        string? route,
        IRequestBody? responseBody,
        CancellationToken cancellationToken = default)
    {
        string normalizedRoute = ModernServerRouteMatcher.NormalizeRoute(route);

        lock (_sync)
        {
            _route = normalizedRoute;
            _responseBody = responseBody?.Duplicate();
        }

        if (Port == port && IsRunning)
        {
            return;
        }

        await StopAsync().ConfigureAwait(false);

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        _listener = listener;
        Port = port;
        _listenTask = Task.Run(() => ListenLoopAsync(listener));
    }

    public async Task StopAsync()
    {
        HttpListener? listener = _listener;
        _listener = null;

        if (listener is not null)
        {
            try { listener.Stop(); }
            catch { }
            try { listener.Close(); }
            catch { }
        }

        if (_listenTask is not null)
        {
            try { await _listenTask.ConfigureAwait(false); }
            catch { }
            _listenTask = null;
        }

        Port = 0;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task ListenLoopAsync(HttpListener listener)
    {
        while (listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(context));
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            string configuredRoute;
            IRequestBody? responseBody;
            lock (_sync)
            {
                configuredRoute = _route;
                responseBody = _responseBody?.Duplicate();
            }

            string requestPath = ModernServerRouteMatcher.NormalizeRoute(context.Request.Url?.AbsolutePath ?? "/");
            string? matchedRoute = ModernServerRouteMatcher.FindBestMatch(requestPath, [configuredRoute]);
            if (matchedRoute is null)
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = $"{ContentTypes.TextPlain}; charset=utf-8";
                byte[] notFoundBytes = Encoding.UTF8.GetBytes("404 - Route not found");
                await context.Response.OutputStream.WriteAsync(notFoundBytes).ConfigureAwait(false);
                context.Response.Close();
                return;
            }

            byte[] bodyBytes;
            using (var memoryStream = new MemoryStream())
            {
                await context.Request.InputStream.CopyToAsync(memoryStream).ConfigureAwait(false);
                bodyBytes = memoryStream.ToArray();
            }

            var headers = new List<HttpHeader>();
            foreach (string? key in context.Request.Headers.AllKeys)
            {
                if (key is not null)
                {
                    headers.Add(new HttpHeader(key, context.Request.Headers[key] ?? string.Empty));
                }
            }

            var queryParameters = new List<QueryParameter>();
            foreach (string? key in context.Request.QueryString.AllKeys)
            {
                if (key is not null)
                {
                    queryParameters.Add(new QueryParameter(key, context.Request.QueryString[key] ?? string.Empty));
                }
            }

            LatestRequest = new SimpleHttpListenerRequest(
                requestPath,
                context.Request.HttpMethod,
                headers.ToArray(),
                queryParameters.ToArray(),
                Encoding.UTF8.GetString(bodyBytes));
            RequestReceived?.Invoke(this, EventArgs.Empty);

            byte[] responseBytes = responseBody?.ToByteArray() ?? [];
            if (!string.IsNullOrWhiteSpace(responseBody?.ContentType))
            {
                context.Response.ContentType = responseBody.ContentType;
            }

            if (responseBytes.Length > 0)
            {
                await context.Response.OutputStream.WriteAsync(responseBytes).ConfigureAwait(false);
            }
        }
        catch { }
        finally
        {
            try { context.Response.Close(); }
            catch { }
        }
    }
}
