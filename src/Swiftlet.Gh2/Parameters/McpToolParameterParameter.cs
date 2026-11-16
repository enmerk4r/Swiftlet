using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Mcp;
using Swiftlet.Runtime;
using Swiftlet.Gh2.Icons;
namespace Swiftlet.Gh2.Parameters;
[IoId(ComponentIds.McpToolParameterParameter)]
public sealed class McpToolParameterParameter : Parameter<McpToolParameter>
{
    public McpToolParameterParameter() : base(new Nomen("MCP Tool Parameter", "A parameter definition for an MCP tool", "Swiftlet", Sections.Mcp), Access.Item) { }
    public McpToolParameterParameter(string name, string userName, string info, Access access = Access.Item) : base(name, userName, info, access) { }
    public McpToolParameterParameter(IReader reader) : base(reader) { }
    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
