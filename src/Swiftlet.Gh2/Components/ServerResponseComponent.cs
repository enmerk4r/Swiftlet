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

[IoId(ComponentIds.ServerResponse)]
public sealed class ServerResponseComponent : SwiftletComponent
{
    public ServerResponseComponent() : base("Server Response", "Sends an HTTP response to a pending request and closes the connection.", Sections.Server) { }
    public ServerResponseComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new ListenerRequestParameter("Request", "R", "The HTTP listener request from Server Input", Access.Item));
        var optional1 = new RequestBodyParameter("Body", "B", "Response body content", Access.Item); inputs.Add(optional1, Requirement.MayBeMissing);
        inputs.AddInteger("Status", "S", "HTTP status code (default: 200)", Access.Item).Set(200);
        var p3 = new HttpHeaderParameter("Headers", "H", "Response headers", Access.Twig); inputs.Add(p3, Requirement.MayBeMissing);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddBoolean("Success", "OK", "True if the response was sent successfully", Access.Item);
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        if (!access.GetItem(0, out ModernServerRequestContext request) || request is null) { access.SetItem(0, false); return; }
        access.GetItem(1, out IRequestBody body); access.GetItem(2, out int status); access.GetTwig<HttpHeader>(3, out var headers);
        bool success = request.TrySetResponse(ModernServerHttpResponse.FromBody(status, body, headers?.NonNullItems));
        if (!success) access.AddWarning("Already responded", "Response has already been sent or connection closed"); access.SetItem(0, success);
    }
}
