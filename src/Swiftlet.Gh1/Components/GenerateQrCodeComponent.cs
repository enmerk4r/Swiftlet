using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;
using Swiftlet.Imaging;
using System.Drawing;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GenerateQrCodeComponent : GH_Component
{
    public GenerateQrCodeComponent()
        : base("Generate QR code", "QR", "Generates a QR code from a string", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.septenary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Text to encode as a QR code (e.g. a URL)", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Pixels", "P", "Pixels per module", GH_ParamAccess.item, 20);
        pManager.AddColourParameter("Dark", "D", "Dark color", GH_ParamAccess.item, Color.Black);
        pManager.AddColourParameter("Light", "L", "Light color", GH_ParamAccess.item, Color.White);
        pManager.AddBooleanParameter("Quiet", "Q", "Draw quiet zones", GH_ParamAccess.item, true);
        pManager[1].Optional = true;
        pManager[2].Optional = true;
        pManager[3].Optional = true;
        pManager[4].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Output bitmap", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string text = string.Empty;
        int pixels = 20;
        Color dark = Color.Black;
        Color light = Color.White;
        bool quiet = true;

        if (!DA.GetData(0, ref text))
        {
            return;
        }

        DA.GetData(1, ref pixels);
        DA.GetData(2, ref dark);
        DA.GetData(3, ref light);
        DA.GetData(4, ref quiet);

        SwiftletImage image = ImageCodec.GenerateQrCode(
            text,
            pixels,
            new SwiftletColor(dark.R, dark.G, dark.B, dark.A),
            new SwiftletColor(light.R, light.G, light.B, light.A),
            quiet);

        DA.SetData(0, new BitmapGoo(image));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("FE44460C-871A-4098-A948-48AA02977410");
}

