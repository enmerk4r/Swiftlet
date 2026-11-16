using Grasshopper2.Components;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.CreateHttpHeader)]
public sealed class CreateHttpHeaderComponent : Component
{
    public CreateHttpHeaderComponent()
        : base(new Nomen("Create Http Header", "Create a new Http Header", "Swiftlet", "2. Request")) { }

    public CreateHttpHeaderComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Key", "Ky", "Header Key");
        inputs.AddText("Value", "Vl", "Header Value");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HttpHeaderParameter("Header", "Hd", "Http Header"));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string key);
        access.GetItem(1, out string value);
        access.SetItem(0, new HttpHeader(key, value));
    }
}
