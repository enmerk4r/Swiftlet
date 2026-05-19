using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public static class McpClientConfigBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public static string Build(string serverName, BridgeLaunchCommand launchCommand)
    {
        Guard.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));
        ArgumentNullException.ThrowIfNull(launchCommand);

        var serverConfig = new JsonObject
        {
            ["type"] = "stdio",
            ["command"] = launchCommand.Command,
            ["args"] = CreateStringArray(launchCommand.Args),
        };

        return BuildMcpServersJson(serverName, serverConfig);
    }

    public static string BuildLmStudio(string serverName, string serverUrl)
    {
        return BuildMcpServersJson(serverName, new JsonObject
        {
            ["url"] = ValidateServerUrl(serverUrl),
        });
    }

    public static string BuildClaudeCode(string serverName, string serverUrl)
    {
        return BuildMcpServersJson(serverName, new JsonObject
        {
            ["type"] = "http",
            ["url"] = ValidateServerUrl(serverUrl),
        });
    }

    public static string BuildVsCode(string serverName, string serverUrl)
    {
        Guard.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        var serverConfig = new JsonObject
        {
            ["type"] = "http",
            ["url"] = ValidateServerUrl(serverUrl),
        };

        var payload = new JsonObject
        {
            ["servers"] = new JsonObject
            {
                [serverName] = serverConfig,
            },
        };

        return SerializeJson(payload);
    }

    public static string BuildCodex(string serverName, string serverUrl)
    {
        Guard.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        string validatedUrl = ValidateServerUrl(serverUrl);
        string escapedServerName = EscapeTomlString(serverName);
        string escapedServerUrl = EscapeTomlString(validatedUrl);

        var builder = new StringBuilder();
        builder.Append("[mcp_servers.\"");
        builder.Append(escapedServerName);
        builder.AppendLine("\"]");
        builder.Append("url = \"");
        builder.Append(escapedServerUrl);
        builder.AppendLine("\"");
        return builder.ToString();
    }

    private static string BuildMcpServersJson(string serverName, JsonObject serverConfig)
    {
        Guard.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        var payload = new JsonObject
        {
            ["mcpServers"] = new JsonObject
            {
                [serverName] = serverConfig,
            },
        };

        return SerializeJson(payload);
    }

    private static string ValidateServerUrl(string serverUrl)
    {
        Guard.ThrowIfNullOrWhiteSpace(serverUrl, nameof(serverUrl));
        return serverUrl;
    }

    private static string SerializeJson(JsonNode payload)
    {
        return payload.ToJsonString(SerializerOptions);
    }

    private static JsonArray CreateStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (string value in values)
        {
            array.Add((JsonNode)JsonValue.Create(value)!);
        }

        return array;
    }

    private static string EscapeTomlString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
