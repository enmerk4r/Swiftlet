using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class Base64ToByteArrayComponent : GH_Component
{
    public Base64ToByteArrayComponent()
        : base("Base64 to Byte Array", "B64BA", "Converts a Base64 encoded string to a byte array", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Base64", "B", "Base64 encoded string", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Output Byte Array", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string text = string.Empty;
        DA.GetData(0, ref text);

        DA.SetData(0, new ByteArrayGoo(Convert.FromBase64String(text)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("792AF514-90FE-45B0-82D1-2BB90C55E813");
}

