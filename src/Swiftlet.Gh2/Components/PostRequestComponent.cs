using GrasshopperIO;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.PostRequest)]
public sealed class PostRequestComponent : HttpRequestComponentBase
{
    public PostRequestComponent() : base("POST Request", "Send a POST request") { }
    public PostRequestComponent(IReader reader) : base(reader) { }
    protected override string? FixedMethod => "POST";
    protected override bool SupportsBody => true;
    protected override string BodyDescription => "POST Body";
}
