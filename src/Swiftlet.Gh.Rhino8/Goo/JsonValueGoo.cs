using System.Text.Json.Nodes;
using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class JsonValueGoo : GH_Goo<JsonValue>
{
    public bool RepresentsJsonNull { get; private set; }

    public override bool IsValid => Value is not null || RepresentsJsonNull;

    public override string TypeName => "JSON Value";

    public override string TypeDescription => "JSON scalar value";

    public JsonValueGoo()
    {
        Value = default!;
    }

    public JsonValueGoo(JsonValue? value)
        : this(value, false)
    {
    }

    private JsonValueGoo(JsonValue? value, bool representsJsonNull)
    {
        Value = JsonNodeCloner.Clone(value) as JsonValue;
        RepresentsJsonNull = representsJsonNull;
    }

    public static JsonValueGoo CreateJsonNull() => new(null, true);

    public override IGH_Goo Duplicate() => new JsonValueGoo(Value, RepresentsJsonNull);

    public override bool CastTo<Q>(ref Q target)
    {
        if (typeof(Q) == typeof(JsonNodeGoo))
        {
            object temp = RepresentsJsonNull
                ? JsonNodeGoo.CreateJsonNull()
                : new JsonNodeGoo(Value);

            target = (Q)temp;
            return true;
        }

        return base.CastTo(ref target);
    }

    public override bool CastFrom(object source)
    {
        if (source is JsonNodeGoo nodeGoo)
        {
            if (nodeGoo.RepresentsJsonNull)
            {
                Value = default!;
                RepresentsJsonNull = true;
                return true;
            }

            if (nodeGoo.Value is JsonValue nodeGooValue)
            {
                Value = JsonNodeCloner.Clone(nodeGooValue) as JsonValue;
                RepresentsJsonNull = false;
                return true;
            }
        }

        return base.CastFrom(source);
    }

    public override string ToString()
    {
        if (RepresentsJsonNull)
        {
            return "JSON Value [null]";
        }

        if (Value is null)
        {
            return "JSON Value";
        }

        string preview = Value.ToJsonString();
        if (preview.Length > 20)
        {
            preview = preview[..20] + "...";
        }

        return $"JSON Value [{preview}]";
    }
}
