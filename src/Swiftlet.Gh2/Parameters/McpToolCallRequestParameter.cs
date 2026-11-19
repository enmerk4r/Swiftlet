using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Mcp;
using Swiftlet.Runtime;
using Swiftlet.Gh2.Icons;
namespace Swiftlet.Gh2.Parameters;
[IoId(ComponentIds.McpToolCallRequestParameter)]
public sealed class McpToolCallRequestParameter : Parameter<ModernMcpToolCallContext>
{
    public McpToolCallRequestParameter() : base(Palette.Entry(nameof(McpToolCallRequestParameter), "MCP Tool Call Request", "Request for a pending MCP tool call", "Swiftlet", Sections.Mcp), Access.Item) { }
    public McpToolCallRequestParameter(string name, string userName, string info, Access access = Access.Item) : base(name, userName, info, access) { }
    public McpToolCallRequestParameter(IReader reader) : base(reader) { }
    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
