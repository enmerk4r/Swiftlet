namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketServerSession : IAsyncDisposable
{
    private readonly ModernWebSocketServer _server;
    private readonly ModernWebSocketServerTransport _transport;

    public ModernWebSocketServerSession(
        ModernWebSocketServer? server = null,
        ModernWebSocketServerTransport? transport = null)
    {
        _server = server ?? new ModernWebSocketServer();
        _transport = transport ?? new ModernWebSocketServerTransport(_server);
    }

    public event EventHandler? StateChanged
    {
        add => _server.StateChanged += value;
        remove => _server.StateChanged -= value;
    }

    public int Port { get; private set; }

    public bool IsRunning => _transport.IsRunning;

    public int ActiveClientCount => _server.ActiveClientCount;

    public string StatusMessage => IsRunning && Port > 0
        ? $"ws://localhost:{Port}/ ({ActiveClientCount} clients)"
        : "Stopped";

    public async Task ReconfigureAsync(int port, CancellationToken cancellationToken = default)
    {
        if (Port == port && IsRunning)
        {
            return;
        }

        Port = port;
        await _transport.StartAsync(port, cancellationToken).ConfigureAwait(false);
    }

    public bool TryDequeueMessage(out ModernWebSocketReceivedMessage? message)
    {
        return _server.TryDequeueMessage(out message);
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
