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

        return ResolveCommand(assemblyDirectory, [serverUrl]);
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

        return ResolveCommand(assemblyDirectory, args);
    }

    private static BridgeLaunchCommand ResolveCommand(string assemblyDirectory, IReadOnlyList<string> args)
    {
        string? bridgePath = FindBridgePath(assemblyDirectory);
        if (bridgePath is null)
        {
            throw new FileNotFoundException(
                $"SwiftletBridge was not found in '{assemblyDirectory}'. " +
                "Expected either a root-level bridge artifact for local builds or a packaged bridge under bridge/<rid>/.");
        }

        if (bridgePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return new BridgeLaunchCommand("dotnet", [bridgePath, .. args]);
        }

        return new BridgeLaunchCommand(bridgePath, args);
    }

    private static string? FindBridgePath(string assemblyDirectory)
    {
        foreach (string candidate in GetCandidates())
        {
            string rootLevelPath = Path.Combine(assemblyDirectory, candidate);
            if (File.Exists(rootLevelPath))
            {
                return rootLevelPath;
            }
        }

        string? packagedBridgeDirectory = GetPackagedBridgeDirectory(assemblyDirectory);
        if (packagedBridgeDirectory is null)
        {
            return null;
        }

        foreach (string candidate in GetCandidates())
        {
            string packagedBridgePath = Path.Combine(packagedBridgeDirectory, candidate);
            if (File.Exists(packagedBridgePath))
            {
                return packagedBridgePath;
            }
        }

        return null;
    }

    private static string? GetPackagedBridgeDirectory(string assemblyDirectory)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(assemblyDirectory, "bridge", "win-x64");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                return Path.Combine(assemblyDirectory, "bridge", "osx-arm64");
            }

            return Path.Combine(assemblyDirectory, "bridge", "osx-x64");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Path.Combine(assemblyDirectory, "bridge", "linux-x64");
        }

        return null;
    }

    private static IReadOnlyList<string> GetCandidates()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsCandidates
            : UnixCandidates;
    }
}
