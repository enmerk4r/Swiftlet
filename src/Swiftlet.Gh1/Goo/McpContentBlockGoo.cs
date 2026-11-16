using Grasshopper.Kernel.Types;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class McpContentBlockGoo : GH_Goo<McpContentBlock>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "MCP Content Block";

    public override string TypeDescription => "A content block for an MCP tool result";

    public McpContentBlockGoo()
    {
        Value = default!;
    }

    public McpContentBlockGoo(McpContentBlock? contentBlock)
    {
        Value = contentBlock is null ? default! : contentBlock.Duplicate();
    }

    public override IGH_Goo Duplicate() => new McpContentBlockGoo(Value);

    public override string ToString() => Value switch
    {
        null => "Null MCP Content Block",
        McpTextContentBlock => "MCP Text Content",
        McpImageContentBlock => "MCP Image Content",
        McpResourceLinkContentBlock => "MCP Resource Link",
        McpEmbeddedTextResourceContentBlock => "MCP Embedded Text Resource",
        McpEmbeddedBinaryResourceContentBlock => "MCP Embedded Binary Resource",
        _ => "MCP Content Block",
    };
}
