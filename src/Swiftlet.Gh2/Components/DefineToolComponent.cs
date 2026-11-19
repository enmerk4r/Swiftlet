using Grasshopper2.Components;
using Grasshopper2.Parameters;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Core.Json;
using Swiftlet.Core.Mcp;
using Swiftlet.Imaging;
using Swiftlet.Runtime;
using Swiftlet.Gh2.Parameters;
namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.DefineTool)]
public sealed class DefineToolComponent : SwiftletComponent
{
    public DefineToolComponent() : base("Define Tool", "Defines one tool that the MCP Server can expose. Use it to describe what the tool is called, what it does, and which arguments it accepts.", Sections.Mcp) { }
    public DefineToolComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Name", "N", "Tool name used by the MCP client when calling this tool. Example: 'compute_area'.", Access.Item);
        inputs.AddText("Description", "D", "Plain-language description of what the tool does and when the AI client should use it.", Access.Item);
        var p2 = new McpToolParameterParameter("Parameters", "P", "Parameter definitions that describe the JSON arguments this tool accepts.", Access.Twig); inputs.Add(p2, Requirement.MayBeMissing);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new McpToolDefinitionParameter("Tool", "T", "Tool definition to connect to MCP Server.", Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out string name); access.GetItem(1, out string description); access.GetTwig<McpToolParameter>(2, out var parameters);
        if (string.IsNullOrWhiteSpace(name)) { access.AddError("Invalid tool", "Tool name cannot be empty"); return; }
        access.SetItem(0, new McpToolDefinition(name, description, parameters?.NonNullItems));
    }
}
