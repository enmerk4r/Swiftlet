using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.DeconstructQueryParam)]
public sealed class DeconstructQueryParamComponent : Component
{
    public DeconstructQueryParamComponent() : base(Palette.Entry(nameof(DeconstructQueryParamComponent), "Deconstruct Query Param", "Deconstruct a Query Param into its constituent parts", "Swiftlet", "3. Send")) { }
    public DeconstructQueryParamComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new QueryParameterParameter("Param", "P", "Query Param to deconstruct", Access.Item));
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Key", "K", "Query Parameter Key", Access.Item);
        outputs.AddText("Value", "V", "Query Parameter Value", Access.Item);
    }
    protected override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        access.GetItem(0, out QueryParameter value);
        access.SetItem(0, value.Key);
        access.SetItem(1, value.Value);
    }
}
