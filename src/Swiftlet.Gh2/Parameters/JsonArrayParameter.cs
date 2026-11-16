using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Json;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.JsonArrayParameter)]
public sealed class JsonArrayParameter : Parameter<JsonArrayData>
{
    public JsonArrayParameter()
        : base(new Nomen("JSON Array", "Container for JSON arrays", "Swiftlet", "4. Read JSON"), Access.Item) { }

    public JsonArrayParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public JsonArrayParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
