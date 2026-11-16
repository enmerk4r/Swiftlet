using GrasshopperIO;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.HttpRequest)]
public sealed class HttpRequestComponent : HttpRequestComponentBase
{
    public HttpRequestComponent() : base("HTTP Request", "A multithreaded component that supports all HTTP methods") { }
    public HttpRequestComponent(IReader reader) : base(reader) { }
    protected override bool SupportsBody => true;
    protected override string BodyDescription => "POST Body";
}
