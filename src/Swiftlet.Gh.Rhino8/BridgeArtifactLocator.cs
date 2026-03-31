using System.ComponentModel;
using System.Diagnostics;
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

        EnsureNativeBridgeIsLaunchable(bridgePath);
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

    private static void EnsureNativeBridgeIsLaunchable(string bridgePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        EnsureNativeBridgeIsExecutable(bridgePath);
        ClearMacQuarantineAttribute(bridgePath);
        AdHocSignMacBinary(bridgePath);
    }

    private static void EnsureNativeBridgeIsExecutable(string bridgePath)
    {
        try
        {
            UnixFileMode currentMode = File.GetUnixFileMode(bridgePath);
            UnixFileMode expectedMode = currentMode |
                                        UnixFileMode.UserExecute |
                                        UnixFileMode.GroupExecute |
                                        UnixFileMode.OtherExecute;

            if (expectedMode != currentMode)
            {
                File.SetUnixFileMode(bridgePath, expectedMode);
            }
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException(
                $"SwiftletBridge at '{bridgePath}' is not executable and Swiftlet could not update its Unix permissions automatically.",
                ex);
        }
    }

    private static void ClearMacQuarantineAttribute(string bridgePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        string bridgeDirectory = Path.GetDirectoryName(bridgePath) ?? bridgePath;
        RunMacXattr("-r", "-d", "com.apple.quarantine", bridgeDirectory);
        RunMacXattr("-d", "com.apple.quarantine", bridgePath);
    }

    private static void RunMacXattr(params string[] arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/xattr",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process
            {
                StartInfo = startInfo,
            };

            if (!process.Start())
            {
                return;
            }

            process.WaitForExit(2000);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
        }
    }

    private static void AdHocSignMacBinary(string bridgePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/codesign",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("--force");
            startInfo.ArgumentList.Add("--sign");
            startInfo.ArgumentList.Add("-");
            startInfo.ArgumentList.Add(bridgePath);

            using var process = new Process
            {
                StartInfo = startInfo,
            };

            if (!process.Start())
            {
                return;
            }

            process.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
        }
    }
}
