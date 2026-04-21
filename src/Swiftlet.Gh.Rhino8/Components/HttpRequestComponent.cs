using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class HttpRequestComponent : HttpRequestComponentBase
{
    public HttpRequestComponent()
        : base("HTTP Request", "REQ", "A multithreaded component that supports all HTTP methods")
    {
    }

    protected override bool SupportsBody => true;

    protected override string BodyDescription => "POST Body";

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override Guid ComponentGuid => new("cc8b1ecf-a2e7-495c-8d41-1e84011633bf");
}
