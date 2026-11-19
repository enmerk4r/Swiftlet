using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class PostRequestComponent : HttpRequestComponentBase
{
    public PostRequestComponent()
        : base("POST Request", "POST", "Send a POST request")
    {
    }

    protected override string? FixedMethod => "POST";

    protected override bool SupportsBody => true;

    protected override string BodyDescription => "POST Body";

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new("18931e0e-bb79-4863-8e0f-4a0f13a137a6");
}
