using Grasshopper2.UI;

namespace Swiftlet.Gh2;

/// <summary>
/// Places every Swiftlet object on the GH2 tab the way GH1 lays out its panels: the GH1 exposure row
/// becomes the GH2 slot and GH1 "hidden" objects stay hidden. The layout itself is generated
/// (Palette.g.cs, from docs/parity.json); every Nomen in this assembly is created through <see cref="Entry"/>.
/// Slot and rank only affect the palette; they are not part of an object's identity (IoId).
/// </summary>
internal static partial class Palette
{
    /// <summary>A Nomen for <paramref name="gh2Class"/> with its GH1 palette slot and rank applied.</summary>
    public static Nomen Entry(string? gh2Class, string name, string info, string chapter, string section)
    {
        var nomen = new Nomen(name, info, chapter, section);
        if ((gh2Class is not null && ByClass.TryGetValue(gh2Class, out var place)) ||
            ByName.TryGetValue(section + "|" + name, out place))
            nomen = nomen.WithSlot(place.Slot).WithRank(place.Rank);
        return nomen;
    }
}
