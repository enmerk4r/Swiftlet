using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Parameters;

[IoId("9f413423-370d-4f17-9ca0-7b44d1395952")]
public sealed class ListenerRequestParameter : Parameter<ModernServerRequestContext>
{
    public ListenerRequestParameter()
        : base(new Nomen("Listener Request", "A live HTTP request and one-shot response handle.", "Swiftlet", "8. Server"), Access.Item) { }
    public ListenerRequestParameter(string name, string userName, string info)
        : base(name, userName, info, Access.Item) { }
    public ListenerRequestParameter(IReader reader) : base(reader) { }
}
