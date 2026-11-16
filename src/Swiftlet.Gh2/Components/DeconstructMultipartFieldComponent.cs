using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.DeconstructMultipartField)]
public sealed class DeconstructMultipartFieldComponent : Component
{
    public DeconstructMultipartFieldComponent() : base(Palette.Entry(nameof(DeconstructMultipartFieldComponent), "Deconstruct Multipart Field", "Deconstruct a multipart field into its constituent parts", "Swiftlet", "3. Send")) { }
    public DeconstructMultipartFieldComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new MultipartFieldParameter("Field", "F", "Multipart field to deconstruct", Access.Item));
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Name", "N", "Field name", Access.Item);
        outputs.AddText("Text", "Tx", "Text content (if text field)", Access.Item);
        outputs.Add(new ByteArrayParameter("Bytes", "By", "Raw bytes", Access.Item));
        outputs.AddText("File Name", "Fn", "File name (if present)", Access.Item);
        outputs.AddText("Content Type", "T", "MIME content type", Access.Item);
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out MultipartField value);
        access.SetItem(0, value.Name); access.SetItem(1, value.IsText ? value.Text ?? string.Empty : string.Empty);
        access.SetItem(2, value.Bytes.ToArray()); access.SetItem(3, value.FileName ?? string.Empty);
        access.SetItem(4, value.ContentType);
    }
}
