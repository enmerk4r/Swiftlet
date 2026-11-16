using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.RequestBodyParameter)]
public sealed class RequestBodyParameter : Parameter<IRequestBody>
{
    public RequestBodyParameter()
        : base(new Nomen("Request Body", "Container for request bodies", "Swiftlet", "2. Request"), Access.Item) { }

    public RequestBodyParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public RequestBodyParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
