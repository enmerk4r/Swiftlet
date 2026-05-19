using Grasshopper.Kernel.Types;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class RequestBodyGoo : GH_Goo<IRequestBody>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "Request Body";

    public override string TypeDescription => "A request or response body";

    public RequestBodyGoo()
    {
        Value = new RequestBodyText();
    }

    public RequestBodyGoo(IRequestBody? body)
    {
        Value = body?.Duplicate() ?? new RequestBodyText();
    }

    public override IGH_Goo Duplicate() => new RequestBodyGoo(Value);

    public override string ToString() => $"REQUEST BODY [ {Value?.ContentType} ]";
}
