using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
namespace Swiftlet.Gh2.Parameters;

[IoId("6b0d67e8-c654-4fe7-8eb6-79af07492523")]
public sealed class HttpResponseDataParameter : Parameter<HttpResponseData>
{
    public HttpResponseDataParameter() : base(new Nomen("Http Response", "HTTP response with metadata", "Swiftlet", "3. Send"), Access.Item) { }
    public HttpResponseDataParameter(string name, string userName, string info, Access access = Access.Item) : base(name, userName, info, access) { }
    public HttpResponseDataParameter(IReader reader) : base(reader) { }
}
