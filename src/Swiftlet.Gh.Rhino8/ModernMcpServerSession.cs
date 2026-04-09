using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Swiftlet.Core.Mcp;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernMcpServerSession : IAsyncDisposable
{
    private readonly object _bridgeIoSync = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ModernMcpToolCallContext>> _pendingCalls =
        new(StringComparer.Ordinal);

    private Process? _bridgeProcess;
    private StreamWriter? _bridgeInput;
    private Task? _bridgeOutputTask;
    private Task? _bridgeErrorTask;

    public ModernMcpServerSession(
        string serverName = "Swiftlet",
        IEnumerable<McpToolDefinition>? tools = null)
    {
        ServerName = string.IsNullOrWhiteSpace(serverName) ? "Swiftlet" : serverName;
        Tools = tools?.Select(static tool => tool.Duplicate()).ToArray() ?? [];
    }

    public event EventHandler? RequestQueued;

    public string ServerName { get; private set; }

    public IReadOnlyList<McpToolDefinition> Tools { get; private set; }

    public int Port { get; private set; }

    public bool IsRunning => _bridgeProcess is { HasExited: false };

    public string? StatusMessage => IsRunning && Port > 0
        ? ModernMcpWorkflow.BuildServerUrl(Port).TrimEnd('/')
        : "Stopped";

    public void Configure(string serverName, IEnumerable<McpToolDefinition>? tools)
    {
        ServerName = string.IsNullOrWhiteSpace(serverName) ? "Swiftlet" : serverName;
        Tools = tools?.Select(static tool => tool.Duplicate()).ToArray() ?? [];

        HashSet<string> currentToolNames = Tools.Select(static tool => tool.Name).ToHashSet(StringComparer.Ordinal);
        foreach (string key in _pendingCalls.Keys)
        {
            if (!currentToolNames.Contains(key))
            {
                _pendingCalls.TryRemove(key, out _);
            }
        }
    }

    public async Task ReconfigureAsync(
        int port,
        string serverName,
        IEnumerable<McpToolDefinition>? tools,
        CancellationToken cancellationToken = default)
    {
        Configure(serverName, tools);

        if (Port != port || !IsRunning)
        {
            await StopAsync().ConfigureAwait(false);
            await StartBridgeAsync(port, cancellationToken).ConfigureAwait(false);
        }

        await SendConfigureAsync(cancellationToken).ConfigureAwait(false);
        Port = port;
    }

    public bool TryDequeuePendingCall(string toolName, out ModernMcpToolCallContext? context)
    {
        context = null;
        if (!_pendingCalls.TryGetValue(toolName, out ConcurrentQueue<ModernMcpToolCallContext>? queue))
        {
            return false;
        }

        return queue.TryDequeue(out context);
    }

    public string GenerateConfig(string assemblyLocation)
    {
        int configPort = Port > 0 ? Port : 3001;
        return ModernMcpWorkflow.GenerateConfig(assemblyLocation, ServerName, configPort);
    }

    public string GenerateConfig(string assemblyLocation, McpClientConfigTarget target)
    {
        int configPort = Port > 0 ? Port : 3001;
        return ModernMcpWorkflow.GenerateConfig(assemblyLocation, ServerName, configPort, target);
    }

    public Task<HostActionResult> ExportConfigAsync(
        IHostServices hostServices,
        string assemblyLocation,
        CancellationToken cancellationToken = default)
    {
        string config = GenerateConfig(assemblyLocation);
        return ModernMcpWorkflow.ExportConfigAsync(hostServices, config, cancellationToken);
    }

    public Task<HostActionResult> ExportConfigAsync(
        IHostServices hostServices,
        string assemblyLocation,
        McpClientConfigTarget target,
        CancellationToken cancellationToken = default)
    {
        string config = GenerateConfig(assemblyLocation, target);
        return ModernMcpWorkflow.ExportConfigAsync(hostServices, config, cancellationToken);
    }

    public async Task StopAsync()
    {
        Process? bridgeProcess = _bridgeProcess;
        StreamWriter? bridgeInput = _bridgeInput;
        Task? bridgeOutputTask = _bridgeOutputTask;
        Task? bridgeErrorTask = _bridgeErrorTask;

        if (bridgeInput is not null)
        {
            try
            {
                TryWriteMessage(new JsonObject
                {
                    ["type"] = "shutdown",
                }, bridgeInput);
            }
            catch
            {
            }

            try
            {
                bridgeInput.Dispose();
            }
            catch
            {
            }
        }

        _bridgeProcess = null;
        _bridgeInput = null;
        _bridgeOutputTask = null;
        _bridgeErrorTask = null;

        if (bridgeProcess is not null)
        {
            try
            {
                await bridgeProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch
            {
                try
                {
                    if (!bridgeProcess.HasExited)
                    {
                        bridgeProcess.Kill(true);
                    }
                }
                catch
                {
                }
            }

            bridgeProcess.Dispose();
        }

        if (bridgeOutputTask is not null)
        {
            try { await bridgeOutputTask.ConfigureAwait(false); } catch { }
        }

        if (bridgeErrorTask is not null)
        {
            try { await bridgeErrorTask.ConfigureAwait(false); } catch { }
        }

        _pendingCalls.Clear();
        Port = 0;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task StartBridgeAsync(int port, CancellationToken cancellationToken)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        string assemblyDirectory = Path.GetDirectoryName(typeof(ModernMcpServerSession).Assembly.Location)
            ?? throw new InvalidOperationException("Could not determine Swiftlet assembly directory.");

        BridgeLaunchCommand launchCommand = new BridgeArtifactLocator().ResolveServerCommand(assemblyDirectory, port);

        var startInfo = new ProcessStartInfo
        {
            FileName = launchCommand.Command,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = assemblyDirectory,
        };
        startInfo.Environment["SWIFTLET_PARENT_PID"] = Environment.ProcessId.ToString();

        foreach (string arg in launchCommand.Args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start SwiftletBridge.");
        }

        var readySource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var diagnostics = new BridgeProcessDiagnostics();
        process.StandardInput.AutoFlush = true;

        _bridgeProcess = process;
        _bridgeInput = process.StandardInput;
        _bridgeOutputTask = Task.Run(() => ReadBridgeOutputAsync(process, readySource, diagnostics), CancellationToken.None);
        _bridgeErrorTask = Task.Run(() => DrainBridgeErrorAsync(process, diagnostics), CancellationToken.None);

        try
        {
            await readySource.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await StopAsync().ConfigureAwait(false);
            throw;
        }
    }

    private Task SendConfigureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = new JsonObject
        {
            ["type"] = "configure",
            ["serverName"] = ServerName,
            ["tools"] = new JsonArray(Tools.Select(static tool => (JsonNode?)tool.ToJson()).ToArray()),
        };

        if (!TryWriteMessage(message))
        {
            throw new InvalidOperationException("Failed to send configuration to SwiftletBridge.");
        }

        return Task.CompletedTask;
    }

    private async Task ReadBridgeOutputAsync(Process process, TaskCompletionSource<bool> readySource, BridgeProcessDiagnostics diagnostics)
    {
        try
        {
            while (await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) is string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                JsonObject? message;
                try
                {
                    message = JsonNode.Parse(line) as JsonObject;
                }
                catch
                {
                    continue;
                }

                if (message is null)
                {
                    continue;
                }

                string? type = message["type"]?.GetValue<string>();
                switch (type)
                {
                    case "ready":
                        readySource.TrySetResult(true);
                        break;

                    case "call_tool":
                        HandleIncomingToolCall(message);
                        break;
                }
            }

            if (!readySource.Task.IsCompleted)
            {
                readySource.TrySetException(diagnostics.CreateStartupException(process, "SwiftletBridge exited before it became ready"));
            }
        }
        catch (Exception ex)
        {
            readySource.TrySetException(ex);
        }
    }

    private async Task DrainBridgeErrorAsync(Process process, BridgeProcessDiagnostics diagnostics)
    {
        try
        {
            while (await process.StandardError.ReadLineAsync().ConfigureAwait(false) is string line)
            {
                diagnostics.RecordStderr(line);
            }
        }
        catch
        {
        }
    }

    private void HandleIncomingToolCall(JsonObject message)
    {
        string? callId = message["callId"]?.GetValue<string>();
        string? toolName = message["toolName"]?.GetValue<string>();
        JsonObject arguments = message["arguments"] as JsonObject ?? [];

        if (string.IsNullOrWhiteSpace(callId) || string.IsNullOrWhiteSpace(toolName))
        {
            return;
        }

        var context = new ModernMcpToolCallContext(
            callId,
            toolName,
            arguments,
            result => TrySendToolResult(callId, result));

        ConcurrentQueue<ModernMcpToolCallContext> queue = _pendingCalls.GetOrAdd(
            toolName,
            static _ => new ConcurrentQueue<ModernMcpToolCallContext>());
        queue.Enqueue(context);
        RequestQueued?.Invoke(this, EventArgs.Empty);
    }

    private bool TrySendToolResult(string callId, McpToolResult result)
    {
        var message = new JsonObject
        {
            ["type"] = "tool_result",
            ["callId"] = callId,
            ["result"] = result.ToJson(),
        };

        return TryWriteMessage(message);
    }

    private bool TryWriteMessage(JsonObject message)
    {
        StreamWriter? writer = _bridgeInput;
        return writer is not null && TryWriteMessage(message, writer);
    }

    private bool TryWriteMessage(JsonObject message, StreamWriter writer)
    {
        lock (_bridgeIoSync)
        {
            if (_bridgeProcess is null || _bridgeProcess.HasExited)
            {
                return false;
            }

            try
            {
                writer.WriteLine(message.ToJsonString());
                writer.Flush();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
