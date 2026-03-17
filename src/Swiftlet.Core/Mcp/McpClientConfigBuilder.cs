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
                    ["command"] = launchCommand.Command,
                    ["args"] = launchCommand.Args,
                },
            },
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }
}
