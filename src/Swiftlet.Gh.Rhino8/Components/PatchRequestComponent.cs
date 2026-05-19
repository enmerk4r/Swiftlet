using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class PatchRequestComponent : HttpRequestComponentBase
{
    public PatchRequestComponent()
        : base("PATCH Request", "PATCH", "Send a PATCH request")
    {
    }

    protected override string? FixedMethod => "PATCH";

    protected override bool SupportsBody => true;

    protected override string BodyDescription => "PATCH Body";

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new("2ebe4cc3-334b-4e80-bd1d-59e66e70da46");
}
