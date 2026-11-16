using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Mcp;
using Swiftlet.Runtime;
using Swiftlet.Gh2.Icons;
namespace Swiftlet.Gh2.Parameters;
[IoId(ComponentIds.McpContentBlockParameter)]
public sealed class McpContentBlockParameter : Parameter<McpContentBlock>
{
    public McpContentBlockParameter() : base(new Nomen("MCP Content Block", "A content block for an MCP tool result", "Swiftlet", Sections.Mcp), Access.Item) { }
    public McpContentBlockParameter(string name, string userName, string info, Access access = Access.Item) : base(name, userName, info, access) { }
    public McpContentBlockParameter(IReader reader) : base(reader) { }
    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
}
