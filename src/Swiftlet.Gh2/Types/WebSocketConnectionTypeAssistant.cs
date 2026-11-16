using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using GrasshopperIO;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Types;

public sealed class WebSocketConnectionTypeAssistant : TypeAssistant<ModernWebSocketConnection>
{
    public WebSocketConnectionTypeAssistant() : base("WebSocket Connection") { }

    // A connection is a live handle: every copy refers to the same socket, as in Grasshopper 1.
    public override ModernWebSocketConnection Copy(ModernWebSocketConnection instance) => instance;

    public override string DescribePrimary(Pear<ModernWebSocketConnection> pear) => pear.Item.ToString();

    public override string Format(ModernWebSocketConnection instance) => instance.ToString();

    // Live network connections cannot survive saving and reopening a definition.
    public override bool Write(IWriter writer, Name location, ModernWebSocketConnection instance) => false;

    public override bool Read(IReader reader, Name location, out ModernWebSocketConnection instance)
    {
        instance = null!;
        return false;
    }
}
