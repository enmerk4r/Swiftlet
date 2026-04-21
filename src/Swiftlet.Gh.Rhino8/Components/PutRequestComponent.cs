using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class PutRequestComponent : HttpRequestComponentBase
{
    public PutRequestComponent()
        : base("PUT Request", "PUT", "Send a PUT request")
    {
    }

    protected override string? FixedMethod => "PUT";

    protected override bool SupportsBody => true;

    protected override string BodyDescription => "PUT Body";

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new("8192683a-a7a2-4c59-84ab-93c1f066b52e");
}
