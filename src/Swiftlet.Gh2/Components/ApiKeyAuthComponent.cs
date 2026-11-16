using Grasshopper2.Components;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.ApiKeyAuth)]
public sealed class ApiKeyAuthComponent : Component
{
    public ApiKeyAuthComponent() : base(Palette.Entry(nameof(ApiKeyAuthComponent), "API Key", "Create a header and a query param (you'll likely need one or the other, not both) for API key auth", "Swiftlet", "1. Auth")) { }
    public ApiKeyAuthComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Key", "K", "Header key for your API auth");
        inputs.AddText("Value", "V", "Your API key value");
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HttpHeaderParameter("Header", "H", "Your Auth Key-Value as an Http Header"));
        outputs.Add(new QueryParameterParameter("Query Param", "P", "Your Auth Key-Value as a URL query param"));
    }
    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string v0);
        access.GetItem(1, out string v1);
        access.SetItem(0, AuthHeaderWorkflow.ApiHeader(v0, v1));
        access.SetItem(1, AuthHeaderWorkflow.ApiQuery(v0, v1));
    }
}
