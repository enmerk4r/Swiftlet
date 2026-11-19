using System.Collections.Concurrent;
using System.Drawing;
using System.Reflection;

namespace Swiftlet.Gh.Rhino8;

internal static class ShellIcons
{
    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string ResourceDirectory = ResolveResourceDirectory();
    private static readonly bool DisableIcons = OperatingSystem.IsLinux();

    public static Bitmap? Logo24() => Cache.GetOrAdd("Logo_24x24.png", LoadBitmap);

    public static Bitmap? Logo16() => Cache.GetOrAdd("Logo_16x16.png", LoadBitmap);

    public static Bitmap? For(Type componentType)
    {
        // Icons are generated from Assets/Icons/svg (build/icons/build_icons.py) and copied to
        // Resources/Icons/<slug>.png; the class -> slug table is shared with the GH2 shell.
        string? slug = Swiftlet.Runtime.IconSlugs.ForClass(componentType.Name);
        string fileName = slug is null ? "Logo_24x24.png" : Path.Combine("Icons", slug + ".png");
        return Cache.GetOrAdd(fileName, LoadBitmap);
    }

    private static Bitmap? LoadBitmap(string fileName)
    {
        if (DisableIcons)
        {
            return null;
        }

        string path = Path.Combine(ResourceDirectory, fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes);
            using var bitmap = new Bitmap(stream);
            return new Bitmap(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveResourceDirectory()
    {
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
        return Path.Combine(assemblyDirectory, "Resources");
    }
}
