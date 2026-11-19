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

[IoId(ComponentIds.DeconstructRequest)]
public sealed class DeconstructRequestComponent : SwiftletComponent
{
    public DeconstructRequestComponent() : base("Deconstruct Request", "Extracts data from an HTTP request context.", Sections.Server) { }
    public DeconstructRequestComponent(IReader reader) : base(reader) { }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new ListenerRequestParameter("Request", "R", "The HTTP listener request from Server Input", Access.Item));
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new ListenerRequestParameter("Request", "R", "Pass-through request for Server Response", Access.Item));
        outputs.AddText("Method", "M", "HTTP method (GET, POST, etc.)", Access.Item);
        outputs.AddText("Route", "Rt", "Request path", Access.Item);
        outputs.Add(new HttpHeaderParameter("Headers", "H", "Request headers", Access.Twig));
        outputs.Add(new QueryParameterParameter("Query", "Q", "Query string parameters", Access.Twig));
        outputs.Add(new RequestBodyParameter("Body", "B", "Request body", Access.Item));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        if (!access.GetItem(0, out ModernServerRequestContext request) || request is null) { access.AddWarning("Invalid request", "No valid request provided"); return; }
        access.SetItem(0, request); access.SetItem(1, request.Method); access.SetItem(2, request.Path);
        access.SetTwig(3, request.Headers.ToArray()); access.SetTwig(4, request.QueryParameters.ToArray()); access.SetItem(5, request.Body.Duplicate());
    }
}
