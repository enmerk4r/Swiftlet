using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Json;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.JsonValueParameter)]
public sealed class JsonValueParameter : Parameter<JsonValueData>
{
    public JsonValueParameter()
        : base(new Nomen("JSON Value", "Container for JSON values", "Swiftlet", "4. Read JSON"), Access.Item) { }

    public JsonValueParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public JsonValueParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
