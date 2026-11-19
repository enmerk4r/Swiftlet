using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Grasshopper2.UI.Icon;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.HttpHeaderParameter)]
public sealed class HttpHeaderParameter : Parameter<HttpHeader>
{
    public HttpHeaderParameter()
        : base(Palette.Entry(nameof(HttpHeaderParameter), "Http Header", "Container for HTTP headers", "Swiftlet", "2. Request"), Access.Item) { }

    public HttpHeaderParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public HttpHeaderParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
