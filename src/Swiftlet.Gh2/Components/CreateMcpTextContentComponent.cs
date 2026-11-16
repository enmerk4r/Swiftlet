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

[IoId(ComponentIds.CreateMcpTextContent)]
public sealed class CreateMcpTextContentComponent : SwiftletComponent
{
    public CreateMcpTextContentComponent() : base("Create MCP Text Content", "Creates a text content block for MCP Tool Response.", Sections.Mcp) { }
    public CreateMcpTextContentComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Text", "T", "Text to show to the MCP client as part of the tool result.", Access.Item);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new McpContentBlockParameter("Content Block", "C", "Text content block for MCP Tool Response.", Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        foreach (var sourceId in Parameters.Input(0).Inputs.Forwards)
        {
            var source = Document?.Objects.FindParameter(sourceId);
            if (source?.State?.Data?.Tree()?.Type?.Namespace == "Swiftlet.Core.Json")
            { access.AddWarning("JSON input", "You might want to use Stringify JSON first :)"); break; }
        }
        if (access.GetItem(0, out string text)) access.SetItem(0, new McpTextContentBlock(text));
    }
}
