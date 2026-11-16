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

[IoId(ComponentIds.CreateMcpImageContent)]
public sealed class CreateMcpImageContentComponent : SwiftletComponent
{
    public CreateMcpImageContentComponent() : base("Create MCP Image Content","Creates an image content block for MCP Tool Response by encoding a Swiftlet bitmap.",Sections.Mcp) { }
    public CreateMcpImageContentComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new BitmapParameter("Bitmap","B","Bitmap to embed in the MCP tool result.",Access.Item));
        inputs.AddText("Format","F","Image encoding format to use for the embedded image. Supported: PNG, JPEG, BMP, GIF, TIFF.",Access.Item).Set("png");
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new McpContentBlockParameter("Content Block","C","Image content block for MCP Tool Response.",Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        try
        {
            if (!access.GetItem(0,out SwiftletImage image) || image is null) return;
        access.GetItem(1,out string format); access.Solution.Token.ThrowIfCancellationRequested();
        access.SetItem(0,McpContentFactory.CreateImage(image,format));
        }
        catch(OperationCanceledException) when(access.Solution.Token.IsCancellationRequested){throw;}
        catch(Exception error){access.AddError("Operation failed",error.Message);}
    }
}
