using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwiftletBridge;

/// <summary>
/// SwiftletBridge - A stdio-to-HTTP bridge for MCP (Model Context Protocol)
///
/// This bridge allows Claude Desktop (which uses stdio-based MCP) to communicate
/// with Swiftlet's HTTP-based MCP server running in Grasshopper.
///
/// Usage: SwiftletBridge.exe [server-url]
///   server-url: The URL of the MCP server (default: http://localhost:3001/mcp/)
///
/// Configuration for Claude Desktop (claude_desktop_config.json):
/// {
///   "mcpServers": {
///     "Swiftlet": {
///       "command": "C:\\path\\to\\SwiftletBridge.exe",
///       "args": ["http://localhost:3001/mcp/"]
///     }
///   }
/// }
/// </summary>
class Program
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static string? _sessionId = null;
    private static string _serverUrl = "http://localhost:3001/mcp/";

    static async Task<int> Main(string[] args)
    {
        // Parse command line arguments
        if (args.Length > 0)
        {
            _serverUrl = args[0];
            // Ensure URL ends with /
            if (!_serverUrl.EndsWith("/"))
                _serverUrl += "/";
        }

        // Set up console for proper encoding
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Long timeout for tool calls

        try
        {
            await RunBridge();
            return 0;
        }
        catch (Exception ex)
        {
            await WriteError($"Bridge error: {ex.Message}");
            return 1;
        }
    }

    static async Task RunBridge()
    {
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var response = await ProcessMessage(line);
                if (response != null)
                {
                    await writer.WriteLineAsync(response);
                }
            }
            catch (Exception ex)
            {
                // Try to extract the request ID for the error response
                object? requestId = null;
                try
                {
                    var request = JObject.Parse(line);
                    requestId = request["id"]?.ToObject<object>();
                }
                catch { }

                var errorResponse = CreateErrorResponse(requestId, -32000, $"Bridge error: {ex.Message}");
                await writer.WriteLineAsync(errorResponse);
            }
        }
    }

    static async Task<string?> ProcessMessage(string jsonMessage)
    {
        JObject? request;
        try
        {
            request = JObject.Parse(jsonMessage);
        }
        catch (JsonException)
        {
            return CreateErrorResponse(null, -32700, "Parse error: Invalid JSON");
        }

        var method = request["method"]?.ToString();
        var requestId = request["id"]?.ToObject<object>();

        // Create HTTP request
        var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _serverUrl)
        {
            Content = content
        };

        // Add session ID header if we have one
        if (!string.IsNullOrEmpty(_sessionId))
        {
            httpRequest.Headers.Add("Mcp-Session-Id", _sessionId);
        }

        // Send request to server
        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await _httpClient.SendAsync(httpRequest);
        }
        catch (HttpRequestException ex)
        {
            return CreateErrorResponse(requestId, -32000, $"Connection error: {ex.Message}. Is the Grasshopper MCP server running?");
        }
        catch (TaskCanceledException)
        {
            return CreateErrorResponse(requestId, -32000, "Request timeout");
        }

        // Extract session ID from response headers (for initialize response)
        if (httpResponse.Headers.TryGetValues("Mcp-Session-Id", out var sessionIds))
        {
            _sessionId = sessionIds.FirstOrDefault();
        }

        // Handle notifications (no response expected)
        if (method == "notifications/initialized" || method == "initialized")
        {
            // Server responds with 202 Accepted, no body
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return null; // No response to write for notifications
            }
        }

        // Read response body
        var responseBody = await httpResponse.Content.ReadAsStringAsync();

        // Parse and re-serialize as compact single-line JSON
        // MCP stdio protocol requires each message to be a single line
        try
        {
            var jsonResponse = JObject.Parse(responseBody);
            return jsonResponse.ToString(Formatting.None);
        }
        catch
        {
            return CreateErrorResponse(requestId, -32000, $"Invalid response from server: {responseBody}");
        }
    }

    static string CreateErrorResponse(object? id, int code, string message)
    {
        var error = new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id != null ? JToken.FromObject(id) : JValue.CreateNull(),
            ["error"] = new JObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
        return error.ToString(Formatting.None);
    }

    static async Task WriteError(string message)
    {
        await Console.Error.WriteLineAsync($"[SwiftletBridge] {message}");
    }
}
