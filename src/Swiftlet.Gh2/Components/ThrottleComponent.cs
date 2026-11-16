using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.Throttle)]
public sealed class ThrottleComponent : Component
{
    public ThrottleComponent() : base(new Nomen("Throttle", "This is a simple passthrough component that lets you add a delay. Useful when sending multiple calls to APIs with limitations on maximum number of requests per minute", "Swiftlet", "3. Send")) { }
    public ThrottleComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddGeneric("Input Data", "I", "Data to pass through", Access.Item, Requirement.MayBeMissing);
        inputs.AddInteger("Delay", "D", "Delay in milliseconds", Access.Item, Requirement.MayBeMissing).Set(0);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddGeneric("Output Data", "O", "Data that passed through unchanged");
    }
    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out object input);
        access.GetItem(1, out int delay);
        SendUtilities.Delay(delay, access.Solution.Token);
        access.SetItem(0, input);
    }
}
