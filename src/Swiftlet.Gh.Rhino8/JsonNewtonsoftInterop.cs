using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Swiftlet.Gh.Rhino8;

internal static class JsonNewtonsoftInterop
{
    public static JToken ToJToken(JsonNode? node, bool representsJsonNull = false)
    {
        return representsJsonNull || node is null
            ? JValue.CreateNull()
            : JToken.Parse(node.ToJsonString());
    }

    public static JsonNodeConversion FromJToken(JToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return new JsonNodeConversion(null, RepresentsJsonNull: true);
        }

        JsonNode? parsed = JsonNode.Parse(token.ToString(Formatting.None));
        return parsed is null
            ? new JsonNodeConversion(null, RepresentsJsonNull: true)
            : new JsonNodeConversion(parsed, RepresentsJsonNull: false);
    }

    public readonly record struct JsonNodeConversion(JsonNode? Node, bool RepresentsJsonNull);
}

internal sealed class JsonGooDynamicMetaObject : DynamicMetaObject
{
    public JsonGooDynamicMetaObject(Expression expression, object value)
        : base(expression, BindingRestrictions.Empty, value)
    {
    }

    public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
    {
        if (binder.Name == nameof(GH_Goo<object>.Value))
        {
            MethodInfo scriptVariableMethod = LimitType.GetMethod(nameof(IGH_Goo.ScriptVariable), Type.EmptyTypes)
                ?? throw new MissingMethodException(LimitType.FullName, nameof(IGH_Goo.ScriptVariable));

            Expression self = Expression.Convert(Expression, LimitType);
            Expression scriptVariable = Expression.Call(self, scriptVariableMethod);
            return new DynamicMetaObject(
                Expression.Convert(scriptVariable, typeof(object)),
                BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        return binder.FallbackGetMember(this);
    }
}
