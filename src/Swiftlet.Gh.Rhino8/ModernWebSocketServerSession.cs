using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json.Nodes;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketServerSession : IAsyncDisposable
{
    private readonly object _bridgeIoSync = new();
    private readonly ConcurrentDictionary<string, ModernWebSocketConnection> _connections = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingSends = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<ModernWebSocketReceivedMessage> _messageQueue = new();

    private Process? _bridgeProcess;
    private StreamWriter? _bridgeInput;
    private Task? _bridgeOutputTask;
    private Task? _bridgeErrorTask;
    private int _activeClientCount;

    public event EventHandler? StateChanged;

    public int Port { get; private set; }

    public bool IsRunning => _bridgeProcess is { HasExited: false };

    public int ActiveClientCount => _activeClientCount;

    public string StatusMessage => IsRunning && Port > 0
        ? $"ws://localhost:{Port}/ ({ActiveClientCount} clients)"
        : "Stopped";

    public async Task ReconfigureAsync(int port, CancellationToken cancellationToken = default)
    {
        if (Port != port || !IsRunning)
        {
            await StopAsync().ConfigureAwait(false);
            await StartBridgeAsync(port, cancellationToken).ConfigureAwait(false);
        }

        Port = port;
    }

    public bool TryDequeueMessage(out ModernWebSocketReceivedMessage? message)
    {
        return _messageQueue.TryDequeue(out message);
    }

    public bool TryGetAnyOpenConnection(out ModernWebSocketConnection? connection)
    {
        foreach (ModernWebSocketConnection candidate in _connections.Values)
        {
            if (candidate.IsOpen)
            {
                connection = candidate;
                return true;
            }
        }

        connection = null;
        return false;
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
                TryWriteMessage(new JsonObject { ["type"] = "shutdown" }, bridgeInput);
            }
            catch
            {
            }

            try { bridgeInput.Dispose(); } catch { }
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

        foreach (TaskCompletionSource<bool> pendingSend in _pendingSends.Values)
        {
            pendingSend.TrySetResult(false);
        }

        foreach (ModernWebSocketConnection connection in _connections.Values)
        {
            connection.UpdateProxyState(WebSocketState.Closed);
        }

        _connections.Clear();
        _messageQueue.Clear();
        _pendingSends.Clear();
        _activeClientCount = 0;
        Port = 0;
        OnStateChanged();
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

        string assemblyDirectory = Path.GetDirectoryName(typeof(ModernWebSocketServerSession).Assembly.Location)
            ?? throw new InvalidOperationException("Could not determine Swiftlet assembly directory.");

        BridgeLaunchCommand launchCommand = new BridgeArtifactLocator().ResolveWebSocketServerCommand(assemblyDirectory, port);

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

                switch (message["type"]?.GetValue<string>())
                {
                    case "ready":
                        readySource.TrySetResult(true);
                        break;

                    case "ws_state":
                        HandleStateMessage(message);
                        break;

                    case "ws_message":
                        HandleIncomingMessage(message);
                        break;

                    case "ws_send_result":
                        HandleSendResult(message);
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

    private void HandleStateMessage(JsonObject message)
    {
        string? connectionId = message["connectionId"]?.GetValue<string>();
        string remoteEndpoint = message["remoteEndpoint"]?.GetValue<string>() ?? string.Empty;
        string localEndpoint = message["localEndpoint"]?.GetValue<string>() ?? string.Empty;
        string? stateText = message["state"]?.GetValue<string>();
        _activeClientCount = message["activeClientCount"]?.GetValue<int>() ?? 0;

        if (!string.IsNullOrWhiteSpace(connectionId))
        {
            ModernWebSocketConnection connection = _connections.GetOrAdd(
                connectionId,
                id => new ModernWebSocketConnection(
                    id,
                    true,
                    remoteEndpoint,
                    localEndpoint,
                    (payload, cancellationToken) => SendProxyMessageAsync(id, payload, cancellationToken)));

            connection.UpdateProxyState(ParseWebSocketState(stateText));
            if (connection.State == WebSocketState.Closed || connection.State == WebSocketState.Aborted)
            {
                _connections.TryRemove(connectionId, out _);
            }
        }

        OnStateChanged();
    }

    private void HandleIncomingMessage(JsonObject message)
    {
        string? connectionId = message["connectionId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        string remoteEndpoint = message["remoteEndpoint"]?.GetValue<string>() ?? string.Empty;
        string localEndpoint = message["localEndpoint"]?.GetValue<string>() ?? string.Empty;
        string payload = message["message"]?.GetValue<string>() ?? string.Empty;

        ModernWebSocketConnection connection = _connections.GetOrAdd(
            connectionId,
            id => new ModernWebSocketConnection(
                id,
                true,
                remoteEndpoint,
                localEndpoint,
                (text, cancellationToken) => SendProxyMessageAsync(id, text, cancellationToken)));

        connection.UpdateProxyState(WebSocketState.Open);
        _messageQueue.Enqueue(new ModernWebSocketReceivedMessage(connection, payload));
        OnStateChanged();
    }

    private void HandleSendResult(JsonObject message)
    {
        string? sendId = message["sendId"]?.GetValue<string>();
        bool success = message["success"]?.GetValue<bool>() == true;
        if (string.IsNullOrWhiteSpace(sendId))
        {
            return;
        }

        if (_pendingSends.TryRemove(sendId, out TaskCompletionSource<bool>? source))
        {
            source.TrySetResult(success);
        }
    }

    private async Task<bool> SendProxyMessageAsync(string connectionId, string payload, CancellationToken cancellationToken)
    {
        string sendId = Guid.NewGuid().ToString("N");
        var responseSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingSends[sendId] = responseSource;

        if (!TryWriteMessage(new JsonObject
        {
            ["type"] = "ws_send",
            ["sendId"] = sendId,
            ["connectionId"] = connectionId,
            ["message"] = payload,
        }))
        {
            _pendingSends.TryRemove(sendId, out _);
            return false;
        }

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(15));
            return await responseSource.Task.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _pendingSends.TryRemove(sendId, out _);
            return false;
        }
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

    private static WebSocketState ParseWebSocketState(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "open" => WebSocketState.Open,
            "connecting" => WebSocketState.Connecting,
            "closesent" => WebSocketState.CloseSent,
            "closereceived" => WebSocketState.CloseReceived,
            "closed" => WebSocketState.Closed,
            "aborted" => WebSocketState.Aborted,
            _ => WebSocketState.None,
        };
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
