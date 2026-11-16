using Grasshopper2.Components;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Hosts.Rhino;
using Grasshopper2.UI.Icon;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.ScreenCaptureViewport)]
public sealed class ScreenCaptureViewportComponent : Component
{
    public ScreenCaptureViewportComponent()
        : base(Palette.Entry(nameof(ScreenCaptureViewportComponent), "Screen Capture Viewport", "Captures a bitmap of the named Rhino viewport.", "Swiftlet", "7. Utilities")) { }

    public ScreenCaptureViewportComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddBoolean("Capture", "Cp", "Set to true to capture the viewport.").Set(false);
        inputs.AddText("Viewport Name", "Vp", "Name of the Rhino viewport to capture.");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new BitmapParameter("Bitmap", "Bm", "Captured viewport bitmap."));
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out bool capture) || !capture)
            return;

        if (!access.GetItem(1, out string viewportName))
            return;

        // The host service marshals the capture onto the Rhino UI thread.
        if (!RhinoViewportCapture.TryCaptureViewport(viewportName, out var image, out string? errorMessage, access.Solution.Token))
        {
            access.AddError("Capture failed", errorMessage ?? $"Viewport '{viewportName}' could not be captured.");
            return;
        }

        access.SetItem(0, image!);
    }
}
