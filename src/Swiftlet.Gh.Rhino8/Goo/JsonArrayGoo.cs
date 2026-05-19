using System.Text.Json.Nodes;
using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class JsonArrayGoo : GH_Goo<JsonArray>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "JSON Array";

    public override string TypeDescription => "JSON array object";

    public JsonArrayGoo()
    {
        Value = new JsonArray();
    }

    public JsonArrayGoo(JsonArray? array)
    {
        Value = JsonNodeCloner.Clone(array) as JsonArray ?? new JsonArray();
    }

    public override IGH_Goo Duplicate() => new JsonArrayGoo(Value);

    public override bool CastTo<Q>(ref Q target)
    {
        if (typeof(Q) == typeof(JsonNodeGoo))
        {
            object temp = new JsonNodeGoo(Value);
            target = (Q)temp;
            return true;
        }

        return base.CastTo(ref target);
    }

    public override bool CastFrom(object source)
    {
        if (source is JsonNodeGoo nodeGoo && nodeGoo.Value is JsonArray array)
        {
            Value = JsonNodeCloner.Clone(array) as JsonArray ?? new JsonArray();
            return true;
        }

        return base.CastFrom(source);
    }

    public override string ToString()
    {
        if (Value is null)
        {
            return "JSON Array";
        }

        return $"JSON Array [{Value.Count} items]";
    }
}
