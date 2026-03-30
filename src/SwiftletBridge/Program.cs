using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SwiftletBridge;

internal static class Program
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5),
    };

    private static readonly object OutputSync = new();
    private static string? _sessionId;
    private static string _serverUrl = "http://localhost:3001/mcp/";

    private static async Task<int> Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        StartParentProcessWatchdog();

        try
        {
            if (args.Length >= 2 &&
                string.Equals(args[0], "serve-http", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[1], out int port))
            {
                await RunHostedServerAsync(port).ConfigureAwait(false);
                return 0;
            }

            if (args.Length >= 2 &&
                string.Equals(args[0], "serve-route-http", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[1], out int routePort))
            {
                await RunHostedRouteHttpAsync(routePort).ConfigureAwait(false);
                return 0;
            }

            if (args.Length >= 2 &&
                string.Equals(args[0], "serve-websocket", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[1], out int wsPort))
            {
                await RunHostedWebSocketAsync(wsPort).ConfigureAwait(false);
                return 0;
            }

            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                _serverUrl = args[0].EndsWith("/", StringComparison.Ordinal) ? args[0] : args[0] + "/";
            }

            await RunProxyAsync().ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            await WriteErrorAsync($"Bridge error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }

    private static async Task RunHostedServerAsync(int port)
    {
        using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        await using var server = new BridgeHostedMcpHttpServer(port, message => WriteMessageAsync(writer, message));

        await server.StartAsync().ConfigureAwait(false);
        await WriteMessageAsync(writer, new JsonObject { ["type"] = "ready" }).ConfigureAwait(false);

        while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonObject? message = ParseJsonObject(line);
            if (message is null)
            {
                continue;
            }

            string? type = message["type"]?.GetValue<string>();
            switch (type)
            {
                case "configure":
                    server.Configure(
                        message["serverName"]?.GetValue<string>() ?? "Swiftlet",
                        message["tools"] as JsonArray);
                    break;

                case "tool_result":
                    string? callId = message["callId"]?.GetValue<string>();
                    JsonObject? result = message["result"] as JsonObject;
                    if (!string.IsNullOrWhiteSpace(callId) && result is not null)
                    {
                        server.TryCompleteToolResult(callId, result);
                    }

                    break;

                case "shutdown":
                    return;
            }
        }
    }

    private static async Task RunHostedRouteHttpAsync(int port)
    {
        using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        await using var server = new BridgeHostedRouteHttpServer(port, message => WriteMessageAsync(writer, message));

        await server.StartAsync().ConfigureAwait(false);
        await WriteMessageAsync(writer, new JsonObject { ["type"] = "ready" }).ConfigureAwait(false);

        while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonObject? message = ParseJsonObject(line);
            if (message is null)
            {
                continue;
            }

            string? type = message["type"]?.GetValue<string>();
            switch (type)
            {
                case "http_response":
                    string? requestId = message["requestId"]?.GetValue<string>();
                    int statusCode = message["statusCode"]?.GetValue<int>() ?? 200;
                    string? contentType = message["contentType"]?.GetValue<string>();
                    string bodyBase64 = message["bodyBase64"]?.GetValue<string>() ?? string.Empty;
                    byte[] bodyBytes = string.IsNullOrEmpty(bodyBase64) ? [] : Convert.FromBase64String(bodyBase64);
                    server.TryCompleteResponse(requestId ?? string.Empty, statusCode, contentType, bodyBytes, message["headers"] as JsonArray);
                    break;

                case "shutdown":
                    return;
            }
        }
    }

    private static async Task RunHostedWebSocketAsync(int port)
    {
        using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        await using var server = new BridgeHostedWebSocketServer(port, message => WriteMessageAsync(writer, message));

        await server.StartAsync().ConfigureAwait(false);
        await WriteMessageAsync(writer, new JsonObject { ["type"] = "ready" }).ConfigureAwait(false);

        while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonObject? message = ParseJsonObject(line);
            if (message is null)
            {
                continue;
            }

            switch (message["type"]?.GetValue<string>())
            {
                case "ws_send":
                    string? sendId = message["sendId"]?.GetValue<string>();
                    string? connectionId = message["connectionId"]?.GetValue<string>();
                    string? payload = message["message"]?.GetValue<string>();
                    bool success = !string.IsNullOrWhiteSpace(connectionId) &&
                                   !string.IsNullOrEmpty(sendId) &&
                                   await server.SendMessageAsync(connectionId, payload ?? string.Empty).ConfigureAwait(false);
                    await WriteMessageAsync(writer, new JsonObject
                    {
                        ["type"] = "ws_send_result",
                        ["sendId"] = sendId,
                        ["success"] = success,
                    }).ConfigureAwait(false);
                    break;

                case "shutdown":
                    return;
            }
        }
    }

    private static async Task RunProxyAsync()
    {
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };

        while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                string? response = await ProcessProxyMessageAsync(line).ConfigureAwait(false);
                if (response is not null)
                {
                    await writer.WriteLineAsync(response).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                JsonNode? requestId = TryExtractRequestId(line);
                await writer.WriteLineAsync(CreateErrorResponse(requestId, -32000, $"Bridge error: {ex.Message}")).ConfigureAwait(false);
            }
        }
    }

    private static void StartParentProcessWatchdog()
    {
        string? rawParentPid = Environment.GetEnvironmentVariable("SWIFTLET_PARENT_PID");
        if (!int.TryParse(rawParentPid, out int parentPid) || parentPid <= 0)
        {
            return;
        }

        try
        {
            Process parent = Process.GetProcessById(parentPid);
            if (parent.HasExited)
            {
                Environment.Exit(0);
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await parent.WaitForExitAsync().ConfigureAwait(false);
                }
                catch
                {
                }

                Environment.Exit(0);
            });
        }
        catch
        {
            Environment.Exit(0);
        }
    }

    private static async Task<string?> ProcessProxyMessageAsync(string jsonMessage)
    {
        JsonObject? request = ParseJsonObject(jsonMessage);
        if (request is null)
        {
            return CreateErrorResponse(null, -32700, "Parse error: Invalid JSON");
        }

        string? method = request["method"]?.GetValue<string>();
        JsonNode? requestId = request["id"]?.DeepClone();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _serverUrl)
        {
            Content = new StringContent(jsonMessage, Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrEmpty(_sessionId))
        {
            httpRequest.Headers.Add("Mcp-Session-Id", _sessionId);
        }

        HttpResponseMessage httpResponse;

        try
        {
            httpResponse = await HttpClient.SendAsync(httpRequest).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return CreateErrorResponse(requestId, -32000, $"Connection error: {ex.Message}. Is the Grasshopper MCP server running?");
        }
        catch (TaskCanceledException)
        {
            return CreateErrorResponse(requestId, -32000, "Request timeout");
        }

        using (httpResponse)
        {
            if (httpResponse.Headers.TryGetValues("Mcp-Session-Id", out IEnumerable<string>? sessionIds))
            {
                _sessionId = sessionIds.FirstOrDefault();
            }

            if ((method == "notifications/initialized" || method == "initialized") &&
                httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return null;
            }

            string responseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            JsonObject? jsonResponse = ParseJsonObject(responseBody);

            return jsonResponse is not null
                ? ToCompactJson(jsonResponse)
                : CreateErrorResponse(requestId, -32000, $"Invalid response from server: {responseBody}");
        }
    }

    private static async Task WriteMessageAsync(StreamWriter writer, JsonObject message)
    {
        lock (OutputSync)
        {
            writer.WriteLine(message.ToJsonString());
            writer.Flush();
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static JsonObject? ParseJsonObject(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonNode? TryExtractRequestId(string json)
    {
        JsonObject? request = ParseJsonObject(json);
        return request?["id"]?.DeepClone();
    }

    private static string CreateErrorResponse(JsonNode? id, int code, string message)
    {
        var error = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
            },
        };

        return ToCompactJson(error);
    }

    private static string ToCompactJson(JsonNode node)
    {
        return node.ToJsonString();
    }

    private static Task WriteErrorAsync(string message)
    {
        return Console.Error.WriteLineAsync($"[SwiftletBridge] {message}");
    }
}
