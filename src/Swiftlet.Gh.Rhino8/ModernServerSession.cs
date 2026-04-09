using System.Diagnostics;
using System.Text.Json.Nodes;
using Swiftlet.Core.Http;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernServerSession : IAsyncDisposable
{
    private readonly object _bridgeIoSync = new();
    private readonly ModernServer _server;

    private Process? _bridgeProcess;
    private StreamWriter? _bridgeInput;
    private Task? _bridgeOutputTask;
    private Task? _bridgeErrorTask;

    public ModernServerSession(IEnumerable<string>? routes = null, ModernServer? server = null)
    {
        _server = server ?? new ModernServer(routes);
        Routes = ModernServerRouteMatcher.NormalizeRoutes(routes);
    }

    public event EventHandler? RequestQueued
    {
        add => _server.RequestQueued += value;
        remove => _server.RequestQueued -= value;
    }

    public IReadOnlyList<string> Routes { get; private set; }

    public int Port { get; private set; }

    public bool IsRunning => _bridgeProcess is { HasExited: false };

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

        if (Port != port || !IsRunning)
        {
            await StopAsync().ConfigureAwait(false);
            await StartBridgeAsync(port, cancellationToken).ConfigureAwait(false);
        }

        Port = port;
    }

    public bool TryDequeuePendingRequest(string route, out ModernServerRequestContext? context)
    {
        return _server.TryDequeuePendingRequest(route, out context);
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

        string assemblyDirectory = Path.GetDirectoryName(typeof(ModernServerSession).Assembly.Location)
            ?? throw new InvalidOperationException("Could not determine Swiftlet assembly directory.");

        BridgeLaunchCommand launchCommand = new BridgeArtifactLocator().ResolveRouteHttpCommand(assemblyDirectory, port);

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

                    case "http_request":
                        _ = Task.Run(() => HandleIncomingRequestAsync(message));
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

    private async Task HandleIncomingRequestAsync(JsonObject message)
    {
        string requestId = message["requestId"]?.GetValue<string>() ?? string.Empty;
        try
        {
            string method = message["method"]?.GetValue<string>() ?? "GET";
            string path = message["path"]?.GetValue<string>() ?? "/";
            string? contentType = message["contentType"]?.GetValue<string>();
            string bodyBase64 = message["bodyBase64"]?.GetValue<string>() ?? string.Empty;
            byte[] bodyBytes = string.IsNullOrEmpty(bodyBase64) ? [] : Convert.FromBase64String(bodyBase64);

            ModernServerHttpResponse response = await _server
                .HandleHttpRequestAsync(
                    method,
                    path,
                    ParseHeaders(message["headers"] as JsonArray),
                    ParseQueryParameters(message["queryParameters"] as JsonArray),
                    new RequestBodyBytes(string.IsNullOrWhiteSpace(contentType) ? ContentTypes.ApplicationOctetStream : contentType, bodyBytes))
                .ConfigureAwait(false);

            TryWriteMessage(new JsonObject
            {
                ["type"] = "http_response",
                ["requestId"] = requestId,
                ["statusCode"] = response.StatusCode,
                ["contentType"] = response.ContentType,
                ["headers"] = new JsonArray(response.Headers.Select(static header => (JsonNode?)new JsonObject
                {
                    ["key"] = header.Key,
                    ["value"] = header.Value,
                }).ToArray()),
                ["bodyBase64"] = Convert.ToBase64String(response.Body),
            });
        }
        catch (Exception ex)
        {
            TryWriteMessage(new JsonObject
            {
                ["type"] = "http_response",
                ["requestId"] = requestId,
                ["statusCode"] = 500,
                ["contentType"] = "text/plain; charset=utf-8",
                ["headers"] = new JsonArray(),
                ["bodyBase64"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ex.ToString())),
            });
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

    private static IReadOnlyList<HttpHeader> ParseHeaders(JsonArray? headers)
    {
        if (headers is null)
        {
            return [];
        }

        return headers
            .OfType<JsonObject>()
            .Select(static header => new HttpHeader(
                header["key"]?.GetValue<string>() ?? string.Empty,
                header["value"]?.GetValue<string>() ?? string.Empty))
            .ToArray();
    }

    private static IReadOnlyList<QueryParameter> ParseQueryParameters(JsonArray? queryParameters)
    {
        if (queryParameters is null)
        {
            return [];
        }

        return queryParameters
            .OfType<JsonObject>()
            .Select(static parameter => new QueryParameter(
                parameter["key"]?.GetValue<string>() ?? string.Empty,
                parameter["value"]?.GetValue<string>() ?? string.Empty))
            .ToArray();
    }
}
