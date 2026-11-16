using GrasshopperIO;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.GetRequest)]
public sealed class GetRequestComponent : HttpRequestComponentBase
{
    public GetRequestComponent() : base("GET Request", "Send a GET request") { }
    public GetRequestComponent(IReader reader) : base(reader) { }
    protected override string? FixedMethod => "GET";
}
