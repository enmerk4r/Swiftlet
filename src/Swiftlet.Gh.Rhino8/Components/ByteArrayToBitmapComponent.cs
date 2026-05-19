using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;
using Swiftlet.Imaging;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ByteArrayToBitmapComponent : GH_Component
{
    public ByteArrayToBitmapComponent()
        : base("Byte Array To Bitmap", "BATBMP", "Converts a byte array to a bitmap", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.septenary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Input Byte Array", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Output bitmap", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ByteArrayGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            return;
        }

        SwiftletImage image = ImageCodec.Load(goo.Value);
        DA.SetData(0, new BitmapGoo(image));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("7E29F95C-56AE-4D28-9290-A51DC4D956F1");
}

