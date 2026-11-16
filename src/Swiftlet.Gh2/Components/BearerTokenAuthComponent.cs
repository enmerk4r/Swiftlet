using Grasshopper2.Components;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.BearerTokenAuth)]
public sealed class BearerTokenAuthComponent : Component
{
    public BearerTokenAuthComponent() : base(Palette.Entry(nameof(BearerTokenAuthComponent), "Bearer Token Auth", "Creates an Authorization header for your Bearer token", "Swiftlet", "1. Auth")) { }
    public BearerTokenAuthComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Token", "T", "Bearer Token");
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HttpHeaderParameter("Header", "H", "Your Authorization heder"));
    }
    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string v0);
        access.SetItem(0, AuthHeaderWorkflow.Bearer(v0));
    }
}
