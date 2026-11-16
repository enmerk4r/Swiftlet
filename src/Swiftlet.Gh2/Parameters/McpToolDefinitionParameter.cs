using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Mcp;
using Swiftlet.Runtime;
using Swiftlet.Gh2.Icons;
namespace Swiftlet.Gh2.Parameters;
[IoId(ComponentIds.McpToolDefinitionParameter)]
public sealed class McpToolDefinitionParameter : Parameter<McpToolDefinition>
{
    public McpToolDefinitionParameter() : base(Palette.Entry(nameof(McpToolDefinitionParameter), "MCP Tool Definition", "A tool definition for an MCP server", "Swiftlet", Sections.Mcp), Access.Item) { }
    public McpToolDefinitionParameter(string name, string userName, string info, Access access = Access.Item) : base(name, userName, info, access) { }
    public McpToolDefinitionParameter(IReader reader) : base(reader) { }
    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
