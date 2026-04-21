using System.Text;
using System.Text.Json;

namespace Swiftlet.Core.Mcp;

public static class McpClientConfigBuilder
{
    public static string Build(string serverName, BridgeLaunchCommand launchCommand)
    {
        Guard.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));
        ArgumentNullException.ThrowIfNull(launchCommand);

        var payload = new Dictionary<string, object?>
        {
            ["mcpServers"] = new Dictionary<string, object?>
            {
                [serverName] = new Dictionary<string, object?>
                {
                    ["type"] = "stdio",
                    ["command"] = launchCommand.Command,
                    ["args"] = launchCommand.Args,
                },
            },
        };

        return SerializeJson(payload);
    }

    public static string BuildLmStudio(string serverName, string serverUrl)
    {
        return BuildMcpServersJson(serverName, new Dictionary<string, object?>
        {
            ["url"] = ValidateServerUrl(serverUrl),
        });
    }

    public static string BuildClaudeCode(string serverName, string serverUrl)
    {
        return BuildMcpServersJson(serverName, new Dictionary<string, object?>
        {
            ["type"] = "http",
            ["url"] = ValidateServerUrl(serverUrl),
        });
    }

    public static string BuildVsCode(string serverName, string serverUrl)
    {
        Guard.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        var payload = new Dictionary<string, object?>
        {
            ["servers"] = new Dictionary<string, object?>
            {
                [serverName] = new Dictionary<string, object?>
                {
                    ["type"] = "http",
                    ["url"] = ValidateServerUrl(serverUrl),
                },
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

    private static string BuildMcpServersJson(string serverName, IReadOnlyDictionary<string, object?> serverConfig)
    {
        Guard.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        var payload = new Dictionary<string, object?>
        {
            ["mcpServers"] = new Dictionary<string, object?>
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

    private static string SerializeJson(object payload)
    {
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }

    private static string EscapeTomlString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
