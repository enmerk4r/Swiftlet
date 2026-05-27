using System.Text.RegularExpressions;

namespace Swiftlet.Gh.Rhino8;

internal static partial class FilePathUtility
{
    public static string NormalizePath(string path)
    {
        return NormalizePath(path, OperatingSystem.IsWindows());
    }

    private static string NormalizePath(string path, bool isWindows)
    {
        string candidate = (path ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        candidate = candidate.Trim('"');
        candidate = Environment.ExpandEnvironmentVariables(candidate);
        if (!isWindows && WindowsDrivePathRegex().IsMatch(candidate))
        {
            throw new ArgumentException(
                "Windows drive paths are only supported on Windows. Use a native path for this runtime.",
                nameof(path));
        }

        return Path.GetFullPath(candidate);
    }

    [GeneratedRegex("^[A-Za-z]:[\\\\/]")]
    private static partial Regex WindowsDrivePathRegex();
}
