using GrasshopperIO;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.DeleteRequest)]
public sealed class DeleteRequestComponent : HttpRequestComponentBase
{
    public DeleteRequestComponent() : base("DELETE Request", "Send a DELETE request") { }
    public DeleteRequestComponent(IReader reader) : base(reader) { }
    protected override string? FixedMethod => "DELETE";
}
