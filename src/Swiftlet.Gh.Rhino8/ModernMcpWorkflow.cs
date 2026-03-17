using Swiftlet.Core.Mcp;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8;

public static class ModernMcpWorkflow
{
    public static string BuildServerUrl(int port)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        return $"http://localhost:{port}/mcp/";
    }

    public static string GenerateConfig(
        string assemblyLocation,
        string serverName,
        int port,
        BridgeArtifactLocator? bridgeLocator = null)
    {
        if (string.IsNullOrWhiteSpace(assemblyLocation))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(assemblyLocation));
        }

        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(serverName));
        }

        string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            throw new InvalidOperationException($"Could not determine assembly directory from '{assemblyLocation}'.");
        }

        BridgeLaunchCommand bridgeCommand = (bridgeLocator ?? new BridgeArtifactLocator())
            .Resolve(assemblyDirectory, BuildServerUrl(port));

        return McpClientConfigBuilder.Build(serverName, bridgeCommand);
    }

    public static async Task<HostActionResult> ExportConfigAsync(
        IHostServices hostServices,
        string config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hostServices);
        if (string.IsNullOrWhiteSpace(config))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(config));
        }

        HostActionResult result = await hostServices.ClipboardService
            .SetTextAsync(config, cancellationToken)
            .ConfigureAwait(false);

        HostNotificationSeverity severity = result.IsSuccess
            ? HostNotificationSeverity.Info
            : result.RequiresManualAction
                ? HostNotificationSeverity.Warning
                : HostNotificationSeverity.Error;

        string message = result.RequiresManualAction
            ? result.ManualActionText ?? result.Message
            : result.Message;

        hostServices.Notifications.Notify(new HostNotification(severity, message));
        return result;
    }
}
