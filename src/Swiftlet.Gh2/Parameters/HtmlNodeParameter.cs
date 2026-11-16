using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using HtmlAgilityPack;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.HtmlNodeParameter)]
public sealed class HtmlNodeParameter : Parameter<HtmlNode>
{
    public HtmlNodeParameter()
        : base(Palette.Entry(nameof(HtmlNodeParameter), "HTML Node", "Container for HTML nodes", "Swiftlet", "5. Read HTML"), Access.Item) { }

    public HtmlNodeParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public HtmlNodeParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
