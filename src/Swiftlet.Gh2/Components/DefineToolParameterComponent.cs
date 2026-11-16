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

[IoId(ComponentIds.DefineToolParameter)]
public sealed class DefineToolParameterComponent : SwiftletComponent
{
    public DefineToolParameterComponent() : base("Define Tool Parameter", "Defines one input parameter for an MCP tool. These parameter definitions become the schema for the tool's JSON arguments.", Sections.Mcp) { }
    public DefineToolParameterComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Name", "N", "JSON property name for this argument.", Access.Item);
        inputs.AddText("Type", "T", "JSON value type for this argument: string, number, integer, boolean, object, or array.", Access.Item).Set("string");
        inputs.AddText("Description", "D", "Plain-language explanation of what this argument means.", Access.Item).Set("");
        inputs.AddBoolean("Required", "R", "If true, the MCP client is expected to provide this argument.", Access.Item).Set(true);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new McpToolParameterParameter("Parameter", "P", "Tool-parameter definition to connect to Define Tool.", Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out string name); access.GetItem(1, out string type); access.GetItem(2, out string description); access.GetItem(3, out bool required);
        if (string.IsNullOrWhiteSpace(name)) { access.AddError("Invalid parameter", "Parameter name cannot be empty"); return; }
        access.SetItem(0, new McpToolParameter(name, type, description, required));
    }
}
