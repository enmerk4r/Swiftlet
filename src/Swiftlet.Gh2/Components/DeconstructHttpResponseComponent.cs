using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.DeconstructHttpResponse)]
public sealed class DeconstructHttpResponseComponent : Component
{
    public DeconstructHttpResponseComponent() : base(new Nomen("Deconstruct Response", "Deconstruct Http Response", "Swiftlet", "3. Send")) { }
    public DeconstructHttpResponseComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HttpResponseDataParameter("Response", "R", "Http Web response to deconstruct", Access.Item));
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Version", "V", "The HTTP message version. The default is 1.1", Access.Item);
        outputs.AddInteger("Status", "S", "Http status code", Access.Item);
        outputs.AddText("Reason", "R", "The reason phrase which typically is sent by servers together with the status code", Access.Item);
        outputs.Add(new HttpHeaderParameter("Headers", "H", "The collection of HTTP response headers", Access.Twig));
        outputs.AddBoolean("IsSuccess", "iS", "Indicates if the HTTP response was successful", Access.Item);
        outputs.AddText("Content", "C", "Response content", Access.Item);
        outputs.Add(new ByteArrayParameter("Byte Array", "A", "Response data as byte array", Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out HttpResponseData value);
        access.SetItem(0, value.Version); access.SetItem(1, value.StatusCode);
        access.SetItem(2, value.ReasonPhrase); access.SetTwig(3, value.Headers.ToArray());
        access.SetItem(4, value.IsSuccessStatusCode); access.SetItem(5, value.Content);
        access.SetItem(6, value.Bytes.ToArray());
    }
}
