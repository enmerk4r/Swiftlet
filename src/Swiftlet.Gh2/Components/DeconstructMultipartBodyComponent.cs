using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.DeconstructMultipartBody)]
public sealed class DeconstructMultipartBodyComponent : Component
{
    public DeconstructMultipartBodyComponent() : base(new Nomen("Deconstruct Multipart Body", "Deconstruct a multipart/form-data body into individual fields", "Swiftlet", "3. Send")) { }
    public DeconstructMultipartBodyComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new RequestBodyParameter("Body", "B", "A multipart/form-data request body", Access.Item));
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new MultipartFieldParameter("Fields", "F", "Individual multipart fields", Access.Twig));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out IRequestBody value);
        try
        {
            var fields = HttpDeconstructionWorkflow.DeconstructMultipart(value);
            if (fields is null) { access.AddWarning("Body", "Body is not multipart/form-data"); return; }
            access.SetTwig(0, fields.Select(field => field.Duplicate()).ToArray());
        }
        catch (FormatException ex)
        {
            access.AddError("Multipart body", $"Failed to parse multipart body: {ex.Message}");
            access.SetTwig(0, Array.Empty<MultipartField>());
        }
    }
}
