using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ScreenCaptureViewportComponent : GH_Component
{
    public ScreenCaptureViewportComponent()
        : base("Screen Capture Viewport", "SCV", "Captures a bitmap of the named Rhino viewport.", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.octonary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddBooleanParameter("Capture", "C", "Set to true to capture the viewport.", GH_ParamAccess.item, false);
        pManager.AddTextParameter("Viewport Name", "V", "Name of the Rhino viewport to capture.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Captured viewport bitmap.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        bool capture = false;
        string viewportName = string.Empty;

        if (!DA.GetData(0, ref capture) || !capture)
        {
            return;
        }

        if (!DA.GetData(1, ref viewportName))
        {
            return;
        }

        if (!RhinoViewportCapture.TryCaptureViewport(viewportName, out var image, out string? errorMessage))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorMessage ?? $"Viewport '{viewportName}' could not be captured.");
            return;
        }

        DA.SetData(0, new BitmapGoo(image));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("808B6C0C-E4D7-4451-984A-D4A77C4C83EB");
}
