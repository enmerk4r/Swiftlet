using Grasshopper.Kernel.Types;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class HttpResponseDataGoo : GH_Goo<HttpResponseData>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "Http Response";

    public override string TypeDescription => "The result of an HTTP request";

    public HttpResponseDataGoo()
    {
        Value = default!;
    }

    public HttpResponseDataGoo(HttpResponseData? response)
    {
        Value = response?.Duplicate() ?? default!;
    }

    public override IGH_Goo Duplicate() => new HttpResponseDataGoo(Value);

    public override string ToString() => $"Http Response [{Value?.StatusCode}]";
}
