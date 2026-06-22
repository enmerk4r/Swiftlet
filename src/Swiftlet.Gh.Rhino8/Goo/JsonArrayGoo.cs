using System.Text.Json.Nodes;
using System.Dynamic;
using System.Linq.Expressions;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class JsonArrayGoo : GH_Goo<JsonArray>, IDynamicMetaObjectProvider
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

    public override object ScriptVariable()
    {
        return JsonNewtonsoftInterop.ToJToken(Value);
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
            object temp = new JsonNodeGoo(Value);
            target = (Q)temp;
            return true;
        }

        if (targetType == typeof(JArray))
        {
            if (JsonNewtonsoftInterop.ToJToken(Value) is JArray array)
            {
                object temp = array;
                target = (Q)temp;
                return true;
            }

            return false;
        }

        if (targetType == typeof(JToken) || targetType == typeof(object))
        {
            object temp = JsonNewtonsoftInterop.ToJToken(Value);
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

        if (source is JArray jArray)
        {
            JsonNewtonsoftInterop.JsonNodeConversion conversion = JsonNewtonsoftInterop.FromJToken(jArray);
            if (conversion.Node is JsonArray arrayFromNewtonsoft)
            {
                Value = arrayFromNewtonsoft;
                return true;
            }
        }

        if (source is JToken token)
        {
            JsonNewtonsoftInterop.JsonNodeConversion conversion = JsonNewtonsoftInterop.FromJToken(token);
            if (conversion.Node is JsonArray arrayFromNewtonsoft)
            {
                Value = arrayFromNewtonsoft;
                return true;
            }
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
