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

    private static string? _sessionId;
    private static string _serverUrl = "http://localhost:3001/mcp/";

    private static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            _serverUrl = args[0].EndsWith("/", StringComparison.Ordinal) ? args[0] : args[0] + "/";
        }

        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            await RunBridgeAsync();
            return 0;
        }
        catch (Exception ex)
        {
            await WriteErrorAsync($"Bridge error: {ex.Message}");
            return 1;
        }
    }

    private static async Task RunBridgeAsync()
    {
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };

        while (await reader.ReadLineAsync() is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                string? response = await ProcessMessageAsync(line);
                if (response is not null)
                {
                    await writer.WriteLineAsync(response);
                }
            }
            catch (Exception ex)
            {
                JsonNode? requestId = TryExtractRequestId(line);
                await writer.WriteLineAsync(CreateErrorResponse(requestId, -32000, $"Bridge error: {ex.Message}"));
            }
        }
    }

    private static async Task<string?> ProcessMessageAsync(string jsonMessage)
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
            httpResponse = await HttpClient.SendAsync(httpRequest);
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

            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            JsonObject? jsonResponse = ParseJsonObject(responseBody);

            return jsonResponse is not null
                ? ToCompactJson(jsonResponse)
                : CreateErrorResponse(requestId, -32000, $"Invalid response from server: {responseBody}");
        }
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
