using Grasshopper2.Doc;
using Grasshopper2.Doc.Migration;
using Grasshopper2.Interop;
using Swiftlet.Gh2.Parameters;

namespace Swiftlet.Gh2.Migration;

// Grasshopper 1 -> Grasshopper 2 rules for the network part of "8. Server". These components keep no
// saved settings (ports, URLs and run switches are ordinary inputs), so identity rules are enough.
// ServerMigrationTable forwards these.
internal static class NetworkMigrationRules
{
    public static IEnumerable<IMigrateBase> Create()
    {
        yield return Gh1MigrationRule.Identity("eb521f46-c8ee-49b3-ad30-0eafb8b2b75e", ComponentIds.HttpListener, 3, 4); // HTTP Listener
        yield return Gh1MigrationRule.Identity("b4e8c3d2-9f5a-4e6b-8c7d-2f3e4a5b6c7d", ComponentIds.UdpListener, 1, 2); // UDP Listener
        yield return Gh1MigrationRule.Identity("1651f788-de9e-4ed5-bfe5-f043b8e37362", ComponentIds.WebSocketClient, 3, 4); // WebSocket Client
        yield return Gh1MigrationRule.Identity("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b", ComponentIds.WebSocketSend, 3, 2); // WebSocket Send
        yield return Gh1MigrationRule.Identity("f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c", ComponentIds.WebSocketServer, 2, 5); // WebSocket Server
    }
}

// Registered by discovery (public class with a parameterless constructor), so it is not also yielded above.
public sealed class WebSocketConnectionMigration : IMigrateParameter
{
    public Guid Grasshopper1Id => new("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a");

    public IDocumentObject Migrate(IGH_Param source)
    {
        var target = new WebSocketConnectionParameter();
        source.Transfer(target);
        return target;
    }
}
