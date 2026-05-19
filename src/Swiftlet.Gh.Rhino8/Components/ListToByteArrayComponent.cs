using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ListToByteArrayComponent : GH_Component
{
    public ListToByteArrayComponent()
        : base("List To Byte Array", "LBA", "Converts a list of integers into a byte array", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Bytes", "B", "Input as a list of integers", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Byte Array", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var values = new List<int>();
        DA.GetDataList(0, values);
        DA.SetData(0, new ByteArrayGoo(values.Select(static value => (byte)value).ToArray()));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("BDFD03F3-42CB-48DB-9F5A-607FCF3143E2");
}

