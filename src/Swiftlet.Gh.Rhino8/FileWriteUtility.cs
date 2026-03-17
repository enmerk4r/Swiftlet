namespace Swiftlet.Gh.Rhino8;

internal static class FileWriteUtility
{
    public static long WriteBytes(string path, byte[] bytes)
    {
        EnsureDirectory(path);
        File.WriteAllBytes(path, bytes ?? Array.Empty<byte>());
        return new FileInfo(path).Length;
    }

    public static long WriteText(string path, string content)
    {
        EnsureDirectory(path);
        File.WriteAllText(path, content ?? string.Empty);
        return new FileInfo(path).Length;
    }

    public static long WriteLines(string path, IEnumerable<string> lines)
    {
        EnsureDirectory(path);
        File.WriteAllLines(path, lines ?? Array.Empty<string>());
        return new FileInfo(path).Length;
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
