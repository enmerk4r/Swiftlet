using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ByteArrayToBase64Component : GH_Component
{
    public ByteArrayToBase64Component()
        : base("Byte Array To Base64", "BAB64", "Converts a Byte Array to a Base64 encoded string", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Input Byte Array", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Base64", "B", "Base64 encoded string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ByteArrayGoo? goo = null;
        DA.GetData(0, ref goo);
        if (goo?.Value is null)
        {
            return;
        }

        DA.SetData(0, Convert.ToBase64String(goo.Value));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("143261E1-C3D1-45B3-B67E-C3A510EB753E");
}

