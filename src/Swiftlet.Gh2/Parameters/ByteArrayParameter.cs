using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.ByteArrayParameter)]
public sealed class ByteArrayParameter : Parameter<byte[]>
{
    public ByteArrayParameter()
        : base(Palette.Entry(nameof(ByteArrayParameter), "Byte Array", "Container for byte arrays", "Swiftlet", "7. Utilities"), Access.Item) { }

    public ByteArrayParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public ByteArrayParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
