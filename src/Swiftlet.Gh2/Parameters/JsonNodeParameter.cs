using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Json;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.JsonNodeParameter)]
public sealed class JsonNodeParameter : Parameter<JsonToken>
{
    public JsonNodeParameter()
        : base(new Nomen("JSON Token", "Container for JSON tokens", "Swiftlet", "4. Read JSON"), Access.Item) { }

    public JsonNodeParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public JsonNodeParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
