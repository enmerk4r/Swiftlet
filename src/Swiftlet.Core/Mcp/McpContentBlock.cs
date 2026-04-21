using System.Text.Json.Nodes;

namespace Swiftlet.Core.Mcp;

public abstract class McpContentBlock
{
    public abstract McpContentBlock Duplicate();

    public abstract JsonObject ToJson();
}
