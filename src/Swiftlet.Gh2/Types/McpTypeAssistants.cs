using System.Text.Json.Nodes;
using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using GrasshopperIO;
using Swiftlet.Core.Mcp;
using Swiftlet.Runtime;
namespace Swiftlet.Gh2.Types;
public sealed class McpContentBlockTypeAssistant : TypeAssistant<McpContentBlock>
{
    public McpContentBlockTypeAssistant() : base("MCP Content Block") { }
    public override McpContentBlock Copy(McpContentBlock instance) => instance.Duplicate();
    public override string DescribePrimary(Pear<McpContentBlock> pear) => Format(pear.Item);
    public override string Format(McpContentBlock instance) => instance switch { McpTextContentBlock => "MCP Text Content", McpImageContentBlock => "MCP Image Content", McpResourceLinkContentBlock => "MCP Resource Link", McpEmbeddedTextResourceContentBlock => "MCP Embedded Text Resource", McpEmbeddedBinaryResourceContentBlock => "MCP Embedded Binary Resource", _ => "MCP Content Block" };
    public override bool Write(IWriter writer, Name location, McpContentBlock instance) { writer.String(location, instance.ToJson().ToJsonString()); return true; }
    public override bool Read(IReader reader, Name location, out McpContentBlock instance)
    {
        instance = null!;
        if (!reader.HasItem(location) || JsonNode.Parse(reader.String(location)) is not JsonObject json) return false;
        instance = McpArchiveCodec.Content(json); return true;
    }
}

public sealed class McpToolParameterTypeAssistant : TypeAssistant<McpToolParameter>
{
    public McpToolParameterTypeAssistant() : base("MCP Tool Parameter") { }
    public override McpToolParameter Copy(McpToolParameter instance) => instance.Duplicate();
    public override string DescribePrimary(Pear<McpToolParameter> pear) => Format(pear.Item);
    public override string Format(McpToolParameter instance) => $"MCP Param: {instance.Name} ({instance.Type})";
    public override bool Write(IWriter writer, Name location, McpToolParameter instance) { writer.String(location, McpArchiveCodec.Parameter(instance).ToJsonString()); return true; }
    public override bool Read(IReader reader, Name location, out McpToolParameter instance)
    {
        instance = null!;
        if (!reader.HasItem(location) || JsonNode.Parse(reader.String(location)) is not JsonObject json) return false;
        instance = McpArchiveCodec.Parameter(json); return true;
    }
}

public sealed class McpToolDefinitionTypeAssistant : TypeAssistant<McpToolDefinition>
{
    public McpToolDefinitionTypeAssistant() : base("MCP Tool Definition") { }
    public override McpToolDefinition Copy(McpToolDefinition instance) => instance.Duplicate();
    public override string DescribePrimary(Pear<McpToolDefinition> pear) => Format(pear.Item);
    public override string Format(McpToolDefinition instance) => $"MCP Tool: {instance.Name}";
    public override bool Write(IWriter writer, Name location, McpToolDefinition instance) { writer.String(location, McpArchiveCodec.Tool(instance).ToJsonString()); return true; }
    public override bool Read(IReader reader, Name location, out McpToolDefinition instance)
    {
        instance = null!;
        if (!reader.HasItem(location) || JsonNode.Parse(reader.String(location)) is not JsonObject json) return false;
        instance = McpArchiveCodec.Tool(json); return true;
    }
}

public sealed class McpToolCallRequestTypeAssistant : TypeAssistant<ModernMcpToolCallContext>
{
    public McpToolCallRequestTypeAssistant() : base("MCP Tool Call Request") { }
    public override ModernMcpToolCallContext Copy(ModernMcpToolCallContext instance) => instance;
    public override string DescribePrimary(Pear<ModernMcpToolCallContext> pear) => Format(pear.Item);
    public override string Format(ModernMcpToolCallContext instance) => $"MCP Call: {instance.ToolName} ({(instance.HasResponded ? "responded" : "pending")})";
    public override bool Write(IWriter writer, Name location, ModernMcpToolCallContext instance) => false;
    public override bool Read(IReader reader, Name location, out ModernMcpToolCallContext instance) { instance = null!; return false; }
}
