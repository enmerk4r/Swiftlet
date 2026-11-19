using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.MultipartFieldParameter)]
public sealed class MultipartFieldParameter : Parameter<MultipartField>
{
    public MultipartFieldParameter()
        : base(Palette.Entry(nameof(MultipartFieldParameter), "Multipart Field", "Container for multipart/form-data fields", "Swiftlet", "2. Request"), Access.Item) { }

    public MultipartFieldParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public MultipartFieldParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
