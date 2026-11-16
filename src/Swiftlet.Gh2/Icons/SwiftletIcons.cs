using System.Collections.Concurrent;
using System.Reflection;
using Grasshopper2.UI.Icon;
using Grasshopper2.UI.Icon.Vector;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Icons;

/// <summary>
/// Vector icons for GH2 objects. The SVG masters in Assets/Icons/svg are embedded as
/// <c>SwiftletIcon.&lt;slug&gt;.svg</c> and looked up through the class -> slug table shared with GH1
/// (<see cref="IconSlugs"/>, generated from Assets/Icons/icons.json).
/// </summary>
/// <remarks>
/// GH2 only auto-binds <c>.ghicon</c> resources named after the class, so every component, parameter
/// and the plugin override <c>IconInternal</c>/<c>Icon</c> with <c>SwiftletIcons.For(GetType())</c>.
/// </remarks>
internal static class SwiftletIcons
{
    private const string ResourcePrefix = "SwiftletIcon.";
    private static readonly ConcurrentDictionary<string, IIcon?> Cache = new(StringComparer.Ordinal);
    private static readonly Assembly Assembly = typeof(SwiftletIcons).Assembly;

    /// <summary>
    /// Icon for a GH2 type. Tries the class name, then the GH1 naming convention for parameters
    /// (<c>FooParameter</c> -> <c>FooParam</c>). Returns null when there is no icon.
    /// </summary>
    public static IIcon? For(Type type)
    {
        string name = type.Name;
        string? slug = IconSlugs.ForClass(name);
        if (slug is null && name.EndsWith("Parameter", StringComparison.Ordinal))
        {
            slug = IconSlugs.ForClass(name[..^"eter".Length]);
        }

        return slug is null ? null : Get(slug);
    }

    /// <summary>Icon by slug (file name in Assets/Icons/svg without extension), or null.</summary>
    public static IIcon? Get(string slug) => Cache.GetOrAdd(slug, Load);

    private static IIcon? Load(string slug)
    {
        try
        {
            using Stream? stream = Assembly.GetManifestResourceStream(ResourcePrefix + slug + ".svg");
            if (stream is null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            return SvgIcon.FromXml(reader.ReadToEnd());
        }
        catch
        {
            // Never let an icon take a component down; GH2 draws its generic icon instead.
            return null;
        }
    }
}
