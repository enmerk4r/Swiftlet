using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetRequestComponent : HttpRequestComponentBase
{
    public GetRequestComponent()
        : base("GET Request", "GET", "Send a GET request")
    {
    }

    protected override string? FixedMethod => "GET";

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new("f2c68f1e-862d-40c0-8bd1-30153ec28c03");
}
