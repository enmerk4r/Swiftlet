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
            "Expected one of: SwiftletBridge, SwiftletBridge.exe, SwiftletBridge.dll");
    }

    private static IReadOnlyList<string> GetCandidates()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsCandidates
            : UnixCandidates;
    }
}
