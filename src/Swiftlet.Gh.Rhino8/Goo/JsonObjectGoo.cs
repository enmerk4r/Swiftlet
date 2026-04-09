using System.Text.Json.Nodes;
using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class JsonObjectGoo : GH_Goo<JsonObject>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "JSON Object";

    public override string TypeDescription => "Searchable JSON object";

    public JsonObjectGoo()
    {
        Value = new JsonObject();
    }

    public JsonObjectGoo(JsonObject? obj)
    {
        Value = obj is null ? new JsonObject() : JsonNodeCloner.CloneObject(obj);
    }

    public override IGH_Goo Duplicate() => new JsonObjectGoo(Value);

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
        if (source is JsonNodeGoo nodeGoo && nodeGoo.Value is JsonObject obj)
        {
            Value = JsonNodeCloner.CloneObject(obj);
            return true;
        }

        return base.CastFrom(source);
    }

    public override string ToString()
    {
        if (Value is null)
        {
            return "JSON Object";
        }

        return $"JSON Object [{Value.Count} keys]";
    }
}
