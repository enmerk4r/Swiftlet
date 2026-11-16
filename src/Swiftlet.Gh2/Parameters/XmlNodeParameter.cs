using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using System.Xml;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.XmlNodeParameter)]
public sealed class XmlNodeParameter : Parameter<XmlNode>
{
    public XmlNodeParameter()
        : base(Palette.Entry(nameof(XmlNodeParameter), "XML Node", "Container for XML nodes", "Swiftlet", "6. Read XML"), Access.Item) { }

    public XmlNodeParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public XmlNodeParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
