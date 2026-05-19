using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ByteArrayToTextComponent : GH_Component
{
    public ByteArrayToTextComponent()
        : base("Byte Array To Text", "BATXT", "Converts a byte array to text", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Input Byte Array", GH_ParamAccess.item);
        pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Output text", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ByteArrayGoo? goo = null;
        string encoding = string.Empty;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref encoding);

        if (goo?.Value is null)
        {
            return;
        }

        try
        {
            DA.SetData(0, UtilityEncoding.Resolve(encoding).GetString(goo.Value));
        }
        catch (ArgumentException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("15E4454A-EE5B-483F-886F-F10307421FF7");
}

