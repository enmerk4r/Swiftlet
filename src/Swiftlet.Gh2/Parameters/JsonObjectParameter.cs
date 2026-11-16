using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Json;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.JsonObjectParameter)]
public sealed class JsonObjectParameter : Parameter<JsonObjectData>
{
    public JsonObjectParameter()
        : base(Palette.Entry(nameof(JsonObjectParameter), "JSON Object", "Container for JSON objects", "Swiftlet", "4. Read JSON"), Access.Item) { }

    public JsonObjectParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public JsonObjectParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
