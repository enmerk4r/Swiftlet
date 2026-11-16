using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using Swiftlet.Gh2.Icons;
using GrasshopperIO;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.ListenerRequestParameter)]
public sealed class ListenerRequestParameter : Parameter<ModernServerRequestContext>
{
    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
    public ListenerRequestParameter()
        : base(new Nomen("Listener Request", "A live HTTP request and one-shot response handle.", "Swiftlet", "8. Server"), Access.Item) { }
    public ListenerRequestParameter(string name, string userName, string info)
        : base(name, userName, info, Access.Item) { }
    public ListenerRequestParameter(IReader reader) : base(reader) { }
}
