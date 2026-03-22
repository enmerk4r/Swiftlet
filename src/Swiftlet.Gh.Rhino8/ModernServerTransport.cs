using System.Net;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernServerTransport : IAsyncDisposable
{
    private readonly ModernServer _server;
    private HttpListener? _listener;
    private Task? _listenTask;

    public ModernServerTransport(ModernServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public bool IsRunning => _listener?.IsListening == true;

    public int Port { get; private set; }

    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
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

            var body = new RequestBodyBytes(
                string.IsNullOrWhiteSpace(context.Request.ContentType)
                    ? ContentTypes.ApplicationOctetStream
                    : context.Request.ContentType,
                bodyBytes);

            string path = context.Request.Url?.AbsolutePath ?? "/";

            ModernServerHttpResponse response = await _server
                .HandleHttpRequestAsync(
                    context.Request.HttpMethod,
                    path,
                    headers,
                    queryParameters,
                    body,
                    CancellationToken.None)
                .ConfigureAwait(false);

            context.Response.StatusCode = response.StatusCode;
            if (!string.IsNullOrWhiteSpace(response.ContentType))
            {
                context.Response.ContentType = response.ContentType;
            }

            foreach (HttpHeader header in response.Headers)
            {
                context.Response.AddHeader(header.Key, header.Value);
            }

            if (response.Body.Length > 0)
            {
                await context.Response.OutputStream.WriteAsync(response.Body).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            try
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = $"{ContentTypes.TextPlain}; charset=utf-8";
                byte[] errorBytes = System.Text.Encoding.UTF8.GetBytes(ex.ToString());
                await context.Response.OutputStream.WriteAsync(errorBytes).ConfigureAwait(false);
            }
            catch { }
        }
        finally
        {
            try { context.Response.Close(); }
            catch { }
        }
    }
}
