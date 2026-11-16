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

[IoId(ComponentIds.CreateMcpResourceLink)]
public sealed class CreateMcpResourceLinkComponent : SwiftletComponent
{
    public CreateMcpResourceLinkComponent() : base("Create MCP Resource Link", "Creates a resource-link content block for MCP Tool Response. Use this when you want to point the client to a resource by URI instead of embedding the resource itself.", Sections.Mcp) { }
    public CreateMcpResourceLinkComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URI", "U", "URI of the linked resource.", Access.Item);
        inputs.AddText("Name", "N", "Short name for the linked resource.", Access.Item);
        inputs.AddText("Title", "T", "Optional display title for the linked resource.", Access.Item, Requirement.MayBeMissing);
        inputs.AddText("Description", "D", "Optional plain-language summary of what the linked resource contains.", Access.Item, Requirement.MayBeMissing);
        inputs.AddText("Mime Type", "M", "Optional MIME type of the linked resource, for example 'image/png' or 'application/json'.", Access.Item, Requirement.MayBeMissing);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new McpContentBlockParameter("Content Block", "C", "Resource-link content block for MCP Tool Response.", Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out string uri); access.GetItem(1, out string name);
        access.GetItem(2, out string title); access.GetItem(3, out string description); access.GetItem(4, out string mime);
        access.SetItem(0, new McpResourceLinkContentBlock(uri, name, title, description, mime));
    }
}
