using System.Runtime.InteropServices;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8;

public sealed class BridgeArtifactLocator
{
    private static readonly string[] WindowsCandidates = ["SwiftletBridge.exe", "SwiftletBridge", "SwiftletBridge.dll"];
    private static readonly string[] UnixCandidates = ["SwiftletBridge", "SwiftletBridge.dll", "SwiftletBridge.exe"];

    public BridgeLaunchCommand Resolve(string assemblyDirectory, string serverUrl)
    {
        if (string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(assemblyDirectory));
        }

        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(serverUrl));
        }

        if (!Directory.Exists(assemblyDirectory))
        {
            throw new DirectoryNotFoundException($"Bridge directory not found: {assemblyDirectory}");
        }

        foreach (string candidate in GetCandidates())
        {
            string fullPath = Path.Combine(assemblyDirectory, candidate);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            if (candidate.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return new BridgeLaunchCommand("dotnet", [fullPath, serverUrl]);
            }

            return new BridgeLaunchCommand(fullPath, [serverUrl]);
        }

        throw new FileNotFoundException(
            $"SwiftletBridge was not found in '{assemblyDirectory}'. " +
            "Expected one of: SwiftletBridge, SwiftletBridge.exe, SwiftletBridge.dll. " +
            "A normal Rhino 8 build should stage the bridge beside the plugin output.");
    }

    public BridgeLaunchCommand ResolveServerCommand(string assemblyDirectory, int port)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        return ResolveForArgs(assemblyDirectory, ["serve-http", port.ToString()]);
    }

    public BridgeLaunchCommand ResolveRouteHttpCommand(string assemblyDirectory, int port)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        return ResolveForArgs(assemblyDirectory, ["serve-route-http", port.ToString()]);
    }

    public BridgeLaunchCommand ResolveWebSocketServerCommand(string assemblyDirectory, int port)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        return ResolveForArgs(assemblyDirectory, ["serve-websocket", port.ToString()]);
    }

    private BridgeLaunchCommand ResolveForArgs(string assemblyDirectory, IReadOnlyList<string> args)
    {
        if (string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(assemblyDirectory));
        }

        if (!Directory.Exists(assemblyDirectory))
        {
            throw new DirectoryNotFoundException($"Bridge directory not found: {assemblyDirectory}");
        }

        foreach (string candidate in GetCandidates())
        {
            string fullPath = Path.Combine(assemblyDirectory, candidate);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            if (candidate.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return new BridgeLaunchCommand("dotnet", [fullPath, .. args]);
            }

            return new BridgeLaunchCommand(fullPath, args);
        }

        throw new FileNotFoundException(
            $"SwiftletBridge was not found in '{assemblyDirectory}'. " +
            "Expected one of: SwiftletBridge, SwiftletBridge.exe, SwiftletBridge.dll. " +
            "A normal Rhino 8 build should stage the bridge beside the plugin output.");
    }

    private static IReadOnlyList<string> GetCandidates()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsCandidates
            : UnixCandidates;
    }
}
