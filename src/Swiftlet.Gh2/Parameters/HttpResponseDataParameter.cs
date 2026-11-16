using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
namespace Swiftlet.Gh2.Parameters;

[IoId("6b0d67e8-c654-4fe7-8eb6-79af07492523")]
public sealed class HttpResponseDataParameter : Parameter<HttpResponseData>
{
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;

    public HttpResponseDataParameter() : base(Palette.Entry(nameof(HttpResponseDataParameter), "Http Response", "Container for HTTP responses", "Swiftlet", "2. Request"), Access.Item) { }
    public HttpResponseDataParameter(string name, string userName, string info, Access access = Access.Item) : base(name, userName, info, access) { }
    public HttpResponseDataParameter(IReader reader) : base(reader) { }
}
