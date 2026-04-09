using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class MultipartFieldParam : GH_Param<MultipartFieldGoo>
{
    public MultipartFieldParam()
        : base("Multipart Field", "MF", "Container for multipart/form-data fields", ShellNaming.Category, ShellNaming.Request, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.senary;

    public override Guid ComponentGuid => new("F5E60C2F-7D03-4A68-B117-848005D73BBC");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

