using Grasshopper2.Components;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.BasicAuth)]
public sealed class BasicAuthComponent : Component
{
    public BasicAuthComponent() : base(Palette.Entry(nameof(BasicAuthComponent), "Basic Auth", "Creates an Authorization header for your Basic auth", "Swiftlet", "1. Auth")) { }
    public BasicAuthComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Username", "U", "Your Basic Auth username");
        inputs.AddText("Password", "P", "Your password. Keep in mind, you're in Grasshopper, so this is kinda sketchy... just sayin'");
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HttpHeaderParameter("Header", "H", "Your Basic Auth header"));
    }
    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string v0);
        access.GetItem(1, out string v1);
        access.SetItem(0, AuthHeaderWorkflow.Basic(v0, v1));
    }
}
