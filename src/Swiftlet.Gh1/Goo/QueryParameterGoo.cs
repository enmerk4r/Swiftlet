using Grasshopper.Kernel.Types;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class QueryParameterGoo : GH_Goo<QueryParameter>
{
    public override bool IsValid => Value is not null && !string.IsNullOrWhiteSpace(Value.Key);

    public override string TypeName => "Query Param";

    public override string TypeDescription => "A query parameter for an HTTP request";

    public QueryParameterGoo()
    {
        Value = default!;
    }

    public QueryParameterGoo(QueryParameter? parameter)
    {
        Value = parameter is null ? default! : new QueryParameter(parameter.Key, parameter.Value);
    }

    public QueryParameterGoo(string key, string value)
    {
        Value = new QueryParameter(key, value);
    }

    public override IGH_Goo Duplicate() => Value is null ? new QueryParameterGoo() : new QueryParameterGoo(Value);

    public override string ToString() => $"PARAM [ {Value?.Key} | {Value?.Value} ]";
}
