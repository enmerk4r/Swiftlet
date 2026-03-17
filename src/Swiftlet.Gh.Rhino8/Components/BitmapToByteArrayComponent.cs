using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;
using Swiftlet.Imaging;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class BitmapToByteArrayComponent : GH_Component
{
    public BitmapToByteArrayComponent()
        : base("Bitmap to Byte Array", "BTBA", "Converts a bitmap to a byte array", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.septenary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Input Bitmap", GH_ParamAccess.item);
        pManager.AddTextParameter("Format", "F", "Image format (BMP, JPEG, PNG, GIF, TIFF, EMF, WMF, EXIF, ICON)", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Output Byte Array", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        BitmapGoo? goo = null;
        string format = string.Empty;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            return;
        }

        DA.GetData(1, ref format);
        if (!ImageFormatParser.TryParse(format, out SwiftletImageFormat imageFormat))
        {
            throw new Exception($"Format {format} is not supported");
        }

        byte[] bytes = ImageCodec.Save(goo.Value, imageFormat);

        DA.SetData(0, new ByteArrayGoo(bytes));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("3511B9B7-DB9F-409C-8CF8-2A67E5BC952D");
}

