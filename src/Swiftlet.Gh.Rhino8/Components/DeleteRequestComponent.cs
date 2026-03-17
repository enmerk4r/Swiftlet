using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeleteRequestComponent : HttpRequestComponentBase
{
    public DeleteRequestComponent()
        : base("DELETE Request", "DELETE", "Send a DELETE request")
    {
    }

    protected override string? FixedMethod => "DELETE";

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new("50043941-dc41-4d1f-8dd8-7cb529c27152");
}
