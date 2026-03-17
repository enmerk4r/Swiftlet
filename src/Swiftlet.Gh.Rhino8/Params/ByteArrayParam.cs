using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class ByteArrayParam : GH_Param<ByteArrayGoo>
{
    public ByteArrayParam()
        : base("Byte Array", "BA", "Container for byte arrays", ShellNaming.Category, ShellNaming.Utilities, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid => new("8C021FD2-744D-4F13-BBE4-94467CF397A1");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

