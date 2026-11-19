using GrasshopperIO;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.PutRequest)]
public sealed class PutRequestComponent : HttpRequestComponentBase
{
    public PutRequestComponent() : base("PUT Request", "Send a PUT request") { }
    public PutRequestComponent(IReader reader) : base(reader) { }
    protected override string? FixedMethod => "PUT";
    protected override bool SupportsBody => true;
    protected override string BodyDescription => "PUT Body";
}
