using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
namespace Swiftlet.Gh2.Parameters;

[IoId("8255113b-b1df-4538-97bc-d4c7b5602b6d")]
public sealed class QueryParameterParameter : Parameter<QueryParameter>
{
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;

    public QueryParameterParameter() : base(new Nomen("Query Parameter", "HTTP query parameter", "Swiftlet", "2. Request"), Access.Item) { }
    public QueryParameterParameter(string name, string userName, string info, Access access = Access.Item) : base(name, userName, info, access) { }
    public QueryParameterParameter(IReader reader) : base(reader) { }
}
