using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.DeconstructBody)]
public sealed class DeconstructBodyComponent : Component
{
    public DeconstructBodyComponent() : base(Palette.Entry(nameof(DeconstructBodyComponent), "Deconstruct Body", "Deconstruct a Request Body into its components", "Swiftlet", "3. Send")) { }
    public DeconstructBodyComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new RequestBodyParameter("Body", "B", "Request body to deconstruct", Access.Item));
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Content Type", "T", "The MIME content type", Access.Item);
        outputs.AddText("Text", "Tx", "Body content as text (UTF-8 decoded)", Access.Item);
        outputs.Add(new ByteArrayParameter("Bytes", "By", "Body content as byte array", Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out IRequestBody value);
        byte[] bytes = value.ToByteArray();
        access.SetItem(0, value.ContentType ?? string.Empty);
        access.SetItem(1, HttpDeconstructionWorkflow.DecodeBodyText(bytes));
        access.SetItem(2, bytes);
    }
}
