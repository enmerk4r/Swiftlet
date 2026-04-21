using System.Text.Json.Nodes;
using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public class JsonNodeGoo : GH_Goo<JsonNode>
{
    public bool RepresentsJsonNull { get; private set; }

    public override bool IsValid => Value is not null || RepresentsJsonNull;

    public override string TypeName => "JSON Token";

    public override string TypeDescription => "Abstract JSON token";

    public JsonNodeGoo()
    {
        Value = default!;
    }

    public JsonNodeGoo(JsonNode? node)
        : this(node, false)
    {
    }

    protected JsonNodeGoo(JsonNode? node, bool representsJsonNull)
    {
        Value = JsonNodeCloner.Clone(node)!;
        RepresentsJsonNull = representsJsonNull;
    }

    public static JsonNodeGoo CreateJsonNull() => new(null, true);

    public override IGH_Goo Duplicate() => new JsonNodeGoo(Value, RepresentsJsonNull);

    public override bool CastTo<Q>(ref Q target)
    {
        Type targetType = typeof(Q);

        if (targetType == typeof(JsonObjectGoo) && Value is JsonObject obj)
        {
            object temp = new JsonObjectGoo(obj);
            target = (Q)temp;
            return true;
        }

        if (targetType == typeof(JsonArrayGoo) && Value is JsonArray array)
        {
            object temp = new JsonArrayGoo(array);
            target = (Q)temp;
            return true;
        }

        if (targetType == typeof(JsonValueGoo))
        {
            object temp = RepresentsJsonNull
                ? JsonValueGoo.CreateJsonNull()
                : new JsonValueGoo(Value as JsonValue);

            target = (Q)temp;
            return RepresentsJsonNull || Value is JsonValue;
        }

        return base.CastTo(ref target);
    }

    public override bool CastFrom(object source)
    {
        switch (source)
        {
            case JsonObjectGoo objectGoo when objectGoo.Value is not null:
                Value = JsonNodeCloner.Clone(objectGoo.Value)!;
                RepresentsJsonNull = false;
                return true;

            case JsonArrayGoo arrayGoo when arrayGoo.Value is not null:
                Value = JsonNodeCloner.Clone(arrayGoo.Value)!;
                RepresentsJsonNull = false;
                return true;

            case JsonValueGoo valueGoo:
                if (valueGoo.RepresentsJsonNull)
                {
                    Value = default!;
                    RepresentsJsonNull = true;
                    return true;
                }

                if (valueGoo.Value is not null)
                {
                    Value = JsonNodeCloner.Clone(valueGoo.Value)!;
                    RepresentsJsonNull = false;
                    return true;
                }

                break;
        }

        return base.CastFrom(source);
    }

    public override string ToString()
    {
        if (RepresentsJsonNull)
        {
            return "JSON Value [null]";
        }

        return Value switch
        {
            JsonObject obj => $"JSON Object [{obj.Count} keys]",
            JsonArray array => $"JSON Array [{array.Count} items]",
            JsonValue value => $"JSON Value [{TrimPreview(value.ToJsonString())}]",
            null => "JSON Token",
            _ => "JSON Token",
        };
    }

    private static string TrimPreview(string text)
    {
        return text.Length > 20 ? text[..20] + "..." : text;
    }
}
