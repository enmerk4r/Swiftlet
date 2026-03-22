using System.Net;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernMcpServerTransport : IAsyncDisposable
{
    private readonly ModernMcpServer _server;
    private HttpListener? _listener;
    private Task? _listenTask;

    public ModernMcpServerTransport(ModernMcpServer server)
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
            string path = context.Request.Url?.AbsolutePath ?? "/";
            if (!path.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            string? sessionId = context.Request.Headers["Mcp-Session-Id"];
            string body;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            ModernMcpHttpResponse response = await _server
                .HandleHttpRequestAsync(context.Request.HttpMethod, sessionId, body, CancellationToken.None)
                .ConfigureAwait(false);

            context.Response.StatusCode = response.StatusCode;

            if (!string.IsNullOrWhiteSpace(response.ContentType))
            {
                context.Response.ContentType = response.ContentType;
            }

            foreach (KeyValuePair<string, string> header in response.Headers)
            {
                context.Response.AddHeader(header.Key, header.Value);
            }

            if (!string.IsNullOrEmpty(response.Body))
            {
                byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response.Body);
                await context.Response.OutputStream.WriteAsync(responseBytes).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            try
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain; charset=utf-8";
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
