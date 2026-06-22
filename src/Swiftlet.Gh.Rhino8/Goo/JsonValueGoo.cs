using System.Text.Json.Nodes;
using System.Dynamic;
using System.Linq.Expressions;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class JsonValueGoo : GH_Goo<JsonValue>, IDynamicMetaObjectProvider
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

    public override object ScriptVariable()
    {
        return JsonNewtonsoftInterop.ToJToken(Value, RepresentsJsonNull);
    }

    public DynamicMetaObject GetMetaObject(Expression parameter)
    {
        return new JsonGooDynamicMetaObject(parameter, this);
    }

    public override bool CastTo<Q>(ref Q target)
    {
        Type targetType = typeof(Q);

        if (targetType == typeof(JsonNodeGoo))
        {
            object temp = RepresentsJsonNull
                ? JsonNodeGoo.CreateJsonNull()
                : new JsonNodeGoo(Value);

            target = (Q)temp;
            return true;
        }

        if (targetType == typeof(JValue) || targetType == typeof(JToken) || targetType == typeof(object))
        {
            object temp = JsonNewtonsoftInterop.ToJToken(Value, RepresentsJsonNull);
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

        if (source is JValue jValue)
        {
            JsonNewtonsoftInterop.JsonNodeConversion conversion = JsonNewtonsoftInterop.FromJToken(jValue);
            if (conversion.RepresentsJsonNull)
            {
                Value = default!;
                RepresentsJsonNull = true;
                return true;
            }

            if (conversion.Node is JsonValue value)
            {
                Value = value;
                RepresentsJsonNull = false;
                return true;
            }
        }

        if (source is JToken token)
        {
            JsonNewtonsoftInterop.JsonNodeConversion conversion = JsonNewtonsoftInterop.FromJToken(token);
            if (conversion.RepresentsJsonNull)
            {
                Value = default!;
                RepresentsJsonNull = true;
                return true;
            }

            if (conversion.Node is JsonValue value)
            {
                Value = value;
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
