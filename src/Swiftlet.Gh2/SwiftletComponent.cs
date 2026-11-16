using Grasshopper2.Components;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2;

/// <summary>
/// Optional base for Swiftlet GH2 components: fixes the chapter to "Swiftlet" and wires the icon.
/// Derived classes still need both constructors (parameterless and IReader).
/// </summary>
public abstract class SwiftletComponent : Component
{
    protected SwiftletComponent(string name, string info, string section)
        : base(Palette.Entry(null, name, info, Sections.Chapter, section)) { }

    protected SwiftletComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}

/// <summary>Chapter and section names; identical to the Grasshopper 1 tab and panels.</summary>
public static class Sections
{
    public const string Chapter = "Swiftlet";
    public const string Auth = "1. Auth";
    public const string Request = "2. Request";
    public const string Send = "3. Send";
    public const string ReadJson = "4. Read JSON";
    public const string ReadHtml = "5. Read HTML";
    public const string ReadXml = "6. Read XML";
    public const string Utilities = "7. Utilities";
    public const string Server = "8. Server";
    public const string Mcp = "9. MCP";
}
