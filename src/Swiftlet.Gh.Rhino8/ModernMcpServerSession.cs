using Swiftlet.Core.Mcp;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernMcpServerSession : IAsyncDisposable
{
    private readonly ModernMcpServer _server;
    private readonly ModernMcpServerTransport _transport;

    public ModernMcpServerSession(
        string serverName = "Swiftlet",
        IEnumerable<McpToolDefinition>? tools = null,
        ModernMcpServer? server = null,
        ModernMcpServerTransport? transport = null)
    {
        _server = server ?? new ModernMcpServer(serverName, tools);
        _transport = transport ?? new ModernMcpServerTransport(_server);
        ServerName = string.IsNullOrWhiteSpace(serverName) ? "Swiftlet" : serverName;
        Tools = tools?.Select(static tool => tool.Duplicate()).ToArray() ?? [];
    }

    public event EventHandler? RequestQueued
    {
        add => _server.RequestQueued += value;
        remove => _server.RequestQueued -= value;
    }

    public string ServerName { get; private set; }

    public IReadOnlyList<McpToolDefinition> Tools { get; private set; }

    public int Port { get; private set; }

    public bool IsRunning => _transport.IsRunning;

    public string? StatusMessage => IsRunning && Port > 0
        ? ModernMcpWorkflow.BuildServerUrl(Port).TrimEnd('/')
        : "Stopped";

    public void Configure(string serverName, IEnumerable<McpToolDefinition>? tools)
    {
        ServerName = string.IsNullOrWhiteSpace(serverName) ? "Swiftlet" : serverName;
        Tools = tools?.Select(static tool => tool.Duplicate()).ToArray() ?? [];
        _server.Configure(ServerName, Tools);
    }

    public async Task ReconfigureAsync(
        int port,
        string serverName,
        IEnumerable<McpToolDefinition>? tools,
        CancellationToken cancellationToken = default)
    {
        Configure(serverName, tools);

        if (Port == port && IsRunning)
        {
            return;
        }

        Port = port;
        await _transport.StartAsync(port, cancellationToken).ConfigureAwait(false);
    }

    public bool TryDequeuePendingCall(string toolName, out ModernMcpToolCallContext? context)
    {
        return _server.TryDequeuePendingCall(toolName, out context);
    }

    public string GenerateConfig(string assemblyLocation)
    {
        int configPort = Port > 0 ? Port : 3001;
        return ModernMcpWorkflow.GenerateConfig(assemblyLocation, ServerName, configPort);
    }

    public Task<HostActionResult> ExportConfigAsync(
        IHostServices hostServices,
        string assemblyLocation,
        CancellationToken cancellationToken = default)
    {
        string config = GenerateConfig(assemblyLocation);
        return ModernMcpWorkflow.ExportConfigAsync(hostServices, config, cancellationToken);
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
