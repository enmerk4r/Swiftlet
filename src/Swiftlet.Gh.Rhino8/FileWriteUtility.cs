namespace Swiftlet.Gh.Rhino8;

internal static class FileWriteUtility
{
    public static long WriteBytes(string path, byte[] bytes)
    {
        string normalizedPath = FilePathUtility.NormalizePath(path);
        EnsureDirectory(normalizedPath);
        File.WriteAllBytes(normalizedPath, bytes ?? Array.Empty<byte>());
        return new FileInfo(normalizedPath).Length;
    }

    public static long WriteText(string path, string content)
    {
        string normalizedPath = FilePathUtility.NormalizePath(path);
        EnsureDirectory(normalizedPath);
        File.WriteAllText(normalizedPath, content ?? string.Empty);
        return new FileInfo(normalizedPath).Length;
    }

    public static long WriteLines(string path, IEnumerable<string> lines)
    {
        string normalizedPath = FilePathUtility.NormalizePath(path);
        EnsureDirectory(normalizedPath);
        File.WriteAllLines(normalizedPath, lines ?? Array.Empty<string>());
        return new FileInfo(normalizedPath).Length;
    }

    private static void EnsureDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
