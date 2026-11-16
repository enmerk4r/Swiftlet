using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.HttpHeaderParameter)]
public sealed class HttpHeaderParameter : Parameter<HttpHeader>
{
    public HttpHeaderParameter()
        : base(new Nomen("Http Header", "Container for HTTP headers", "Swiftlet", "2. Request"), Access.Item) { }

    public HttpHeaderParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public HttpHeaderParameter(IReader reader) : base(reader) { }
}
