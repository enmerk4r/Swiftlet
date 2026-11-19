using Grasshopper2.Components;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Icons;
using Swiftlet.Gh2.Parameters;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.CreateQueryParam)]
public sealed class CreateQueryParamComponent : Component
{
    public CreateQueryParamComponent()
        : base(Palette.Entry(nameof(CreateQueryParamComponent), "Create Query Param", "Create an Http Query Param", "Swiftlet", "2. Request")) { }

    public CreateQueryParamComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Key", "K", "Query Parameter Key");
        inputs.AddText("Value", "V", "Query Parameter Value");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new QueryParameterParameter("Param", "P", "Http Query Parameter"));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string key);
        access.GetItem(1, out string value);
        access.SetItem(0, new QueryParameter(key, value));
    }
}
