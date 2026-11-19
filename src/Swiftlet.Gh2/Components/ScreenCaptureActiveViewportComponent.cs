using Grasshopper2.Components;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Hosts.Rhino;
using Grasshopper2.UI.Icon;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.ScreenCaptureActiveViewport)]
public sealed class ScreenCaptureActiveViewportComponent : Component
{
    public ScreenCaptureActiveViewportComponent()
        : base(Palette.Entry(nameof(ScreenCaptureActiveViewportComponent), "Screen Capture Active Viewport", "Captures a bitmap of the active Rhino viewport.", "Swiftlet", "7. Utilities")) { }

    public ScreenCaptureActiveViewportComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddBoolean("Capture", "C", "Set to true to capture the active viewport.").Set(false);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new BitmapParameter("Bitmap", "B", "Captured active viewport bitmap."));
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out bool capture) || !capture)
            return;

        // The host service marshals the capture onto the Rhino UI thread.
        if (!RhinoViewportCapture.TryCaptureActiveViewport(out var image, out string? errorMessage, access.Solution.Token))
        {
            access.AddError("Capture failed", errorMessage ?? "Active viewport could not be captured.");
            return;
        }

        access.SetItem(0, image!);
    }
}
