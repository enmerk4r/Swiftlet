using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.DeconstructUrl)]
public sealed class DeconstructUrlComponent : Component
{
    public DeconstructUrlComponent() : base(new Nomen("Deconstruct URL", "Deconstruct a URL into its constituent parts", "Swiftlet", "3. Send")) { }
    public DeconstructUrlComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URL", "U", "A URL string to deconstruct", Access.Item);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Base", "B", "Base URL", Access.Item);
        outputs.AddText("Scheme", "S", "URL scheme (http or https)", Access.Item);
        outputs.AddText("Host", "H", "Host component of the URL", Access.Item);
        outputs.AddText("Route", "R", "A route (path) to the online resource", Access.Item);
        outputs.Add(new QueryParameterParameter("Params", "P", "Http Query Parameters", Access.Twig));
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out string url);
        var parts = HttpDeconstructionWorkflow.DeconstructUrl(url);
        access.SetItem(0, parts.Base); access.SetItem(1, parts.Scheme);
        access.SetItem(2, parts.Host); access.SetItem(3, parts.Route);
        access.SetTwig(4, parts.Parameters);
    }
}
