using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Gh2.Icons;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Parameters;

[IoId(ComponentIds.WebSocketConnectionParameter)]
public sealed class WebSocketConnectionParameter : Parameter<ModernWebSocketConnection>
{
    public WebSocketConnectionParameter()
        : base(new Nomen("WebSocket Connection", "An open WebSocket connection", "Swiftlet", "8. Server"), Access.Item) { }

    public WebSocketConnectionParameter(string name, string userName, string info, Access access = Access.Item)
        : base(name, userName, info, access) { }

    public WebSocketConnectionParameter(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
