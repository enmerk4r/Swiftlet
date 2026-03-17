namespace Swiftlet.Gh.Rhino8;

public sealed class ModernServerSession : IAsyncDisposable
{
    private readonly ModernServer _server;
    private readonly ModernServerTransport _transport;

    public ModernServerSession(
        IEnumerable<string>? routes = null,
        ModernServer? server = null,
        ModernServerTransport? transport = null)
    {
        _server = server ?? new ModernServer(routes);
        _transport = transport ?? new ModernServerTransport(_server);
        Routes = ModernServerRouteMatcher.NormalizeRoutes(routes);
    }

    public event EventHandler? RequestQueued
    {
        add => _server.RequestQueued += value;
        remove => _server.RequestQueued -= value;
    }

    public IReadOnlyList<string> Routes { get; private set; }

    public int Port { get; private set; }

    public bool IsRunning => _transport.IsRunning;

    public string StatusMessage => IsRunning && Port > 0
        ? $"http://localhost:{Port}/"
        : "Stopped";

    public void ConfigureRoutes(IEnumerable<string>? routes)
    {
        Routes = ModernServerRouteMatcher.NormalizeRoutes(routes);
        _server.ConfigureRoutes(Routes);
    }

    public async Task ReconfigureAsync(int port, IEnumerable<string>? routes, CancellationToken cancellationToken = default)
    {
        ConfigureRoutes(routes);

        if (Port == port && IsRunning)
        {
            return;
        }

        Port = port;
        await _transport.StartAsync(port, cancellationToken).ConfigureAwait(false);
    }

    public bool TryDequeuePendingRequest(string route, out ModernServerRequestContext? context)
    {
        return _server.TryDequeuePendingRequest(route, out context);
    }

    public async Task StopAsync()
    {
        await _transport.StopAsync().ConfigureAwait(false);
        Port = 0;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
