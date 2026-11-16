using Grasshopper2.Doc;
using Grasshopper2.Doc.Migration;
using Grasshopper2.Interop;
using Swiftlet.Gh2.Components;
using Swiftlet.Gh2.Parameters;

namespace Swiftlet.Gh2.Migration;

public sealed class McpServerMigration : IMigrateComponent
{
    public Guid Grasshopper1Id => new("d4e5f6a7-b8c9-0123-def0-234567890123");
    public IDocumentObject Migrate(IGH_Component source)
    {
        var target = new McpServerComponent();
        var parameters = source.UnderlyingObject.GetType().GetProperty("Params")!.GetValue(source.UnderlyingObject)!;
        var outputs = (System.Collections.IList)parameters.GetType().GetProperty("Output")!.GetValue(parameters)!;
        foreach (object output in outputs)
        {
            string name = (string)output.GetType().GetProperty("NickName")!.GetValue(output)!;
            target.Parameters.AddOutput(new McpToolCallRequestParameter(name, name, "MCP tool call request."), null!);
        }
        source.TransferObjectName(target);
        source.TransferObjectState(target);
        source.TransferInputs(target, [(0, 0), (1, 1), (2, 2)]);
        source.TransferOutputs(target, Enumerable.Range(0, source.OutputCount).Select(index => (index, index)).ToArray());
        for (int index = 0; index < outputs.Count; index++)
            target.Parameters.Output(index).UserName = (string)outputs[index]!.GetType().GetProperty("NickName")!.GetValue(outputs[index])!;
        return target;
    }
}
