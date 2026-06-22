using System.Text.Json.Nodes;
using System.Dynamic;
using System.Linq.Expressions;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class JsonObjectGoo : GH_Goo<JsonObject>, IDynamicMetaObjectProvider
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

        if (targetType == typeof(JObject))
        {
            if (JsonNewtonsoftInterop.ToJToken(Value) is JObject obj)
            {
                object temp = obj;
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
        if (source is JsonNodeGoo nodeGoo && nodeGoo.Value is JsonObject obj)
        {
            Value = JsonNodeCloner.CloneObject(obj);
            return true;
        }

        if (source is JObject jObject)
        {
            JsonNewtonsoftInterop.JsonNodeConversion conversion = JsonNewtonsoftInterop.FromJToken(jObject);
            if (conversion.Node is JsonObject objectFromNewtonsoft)
            {
                Value = objectFromNewtonsoft;
                return true;
            }
        }

        if (source is JToken token)
        {
            JsonNewtonsoftInterop.JsonNodeConversion conversion = JsonNewtonsoftInterop.FromJToken(token);
            if (conversion.Node is JsonObject objectFromNewtonsoft)
            {
                Value = objectFromNewtonsoft;
                return true;
            }
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
