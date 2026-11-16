using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Imaging;
using Grasshopper2.UI.Icon;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.BitmapParameter)]
public sealed class BitmapParameter : Parameter<SwiftletImage>
{
    public BitmapParameter()
        : base(new Nomen("Bitmap", "Container for bitmaps", "Swiftlet", "7. Utilities"), Access.Item) { }

    public BitmapParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public BitmapParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
