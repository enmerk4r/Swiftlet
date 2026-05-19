using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class BitmapParam : GH_Param<BitmapGoo>
{
    public BitmapParam()
        : base("Bitmap", "BMP", "Container for bitmaps", ShellNaming.Category, ShellNaming.Utilities, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid => new("C1CDD3A5-FAAF-484A-978B-307139EBF7F4");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

