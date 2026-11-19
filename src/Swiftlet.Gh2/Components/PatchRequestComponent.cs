using GrasshopperIO;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.PatchRequest)]
public sealed class PatchRequestComponent : HttpRequestComponentBase
{
    public PatchRequestComponent() : base("PATCH Request", "Send a PATCH request") { }
    public PatchRequestComponent(IReader reader) : base(reader) { }
    protected override string? FixedMethod => "PATCH";
    protected override bool SupportsBody => true;
    protected override string BodyDescription => "PATCH Body";
}
