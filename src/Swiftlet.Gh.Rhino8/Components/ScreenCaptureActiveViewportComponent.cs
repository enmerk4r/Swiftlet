using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ScreenCaptureActiveViewportComponent : GH_Component
{
    public ScreenCaptureActiveViewportComponent()
        : base("Screen Capture Active Viewport", "SCAV", "Captures a bitmap of the active Rhino viewport.", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.octonary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddBooleanParameter("Capture", "C", "Set to true to capture the active viewport.", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Captured active viewport bitmap.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        bool capture = false;
        if (!DA.GetData(0, ref capture) || !capture)
        {
            return;
        }

        if (!RhinoViewportCapture.TryCaptureActiveViewport(out var image, out string? errorMessage))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorMessage ?? "Active viewport could not be captured.");
            return;
        }

        DA.SetData(0, new BitmapGoo(image));
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override Guid ComponentGuid => new("AA328F38-14DC-4DE4-B52B-D8CC0288CB7B");
}
