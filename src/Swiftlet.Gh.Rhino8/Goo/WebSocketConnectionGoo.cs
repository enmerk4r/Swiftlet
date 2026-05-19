using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class WebSocketConnectionGoo : GH_Goo<ModernWebSocketConnection>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "WebSocket Connection";

    public override string TypeDescription => "An open WebSocket connection that can be used to send and receive messages";

    public WebSocketConnectionGoo()
    {
        Value = default!;
    }

    public WebSocketConnectionGoo(ModernWebSocketConnection? connection)
    {
        Value = connection ?? default!;
    }

    public override IGH_Goo Duplicate() => new WebSocketConnectionGoo(Value);

    public override string ToString() => Value is null ? "No WebSocket Connection" : Value.ToString();

    public override bool CastTo<Q>(ref Q target)
    {
        if (typeof(Q).IsAssignableFrom(typeof(ModernWebSocketConnection)))
        {
            target = (Q)(object)Value;
            return true;
        }

        if (typeof(Q).IsAssignableFrom(typeof(string)))
        {
            target = (Q)(object)ToString();
            return true;
        }

        return false;
    }

    public override bool CastFrom(object source)
    {
        if (source is null)
        {
            return false;
        }

        if (source is ModernWebSocketConnection connection)
        {
            Value = connection;
            return true;
        }

        if (source is WebSocketConnectionGoo goo)
        {
            Value = goo.Value;
            return true;
        }

        return false;
    }
}
