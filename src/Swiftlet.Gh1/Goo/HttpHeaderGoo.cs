using Grasshopper.Kernel.Types;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class HttpHeaderGoo : GH_Goo<HttpHeader>
{
    public override bool IsValid => Value is not null && !string.IsNullOrWhiteSpace(Value.Key);

    public override string TypeName => "Http Header";

    public override string TypeDescription => "A header for an HTTP request";

    public HttpHeaderGoo()
    {
        Value = default!;
    }

    public HttpHeaderGoo(HttpHeader? header)
    {
        Value = header is null ? default! : new HttpHeader(header.Key, header.Value);
    }

    public HttpHeaderGoo(string key, string value)
    {
        Value = new HttpHeader(key, value);
    }

    public override IGH_Goo Duplicate() => Value is null ? new HttpHeaderGoo() : new HttpHeaderGoo(Value);

    public override string ToString() => $"HEADER [ {Value?.Key} | {Value?.Value} ]";
}
