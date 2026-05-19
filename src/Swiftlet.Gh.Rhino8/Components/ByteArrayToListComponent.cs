using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ByteArrayToListComponent : GH_Component
{
    public ByteArrayToListComponent()
        : base("Byte Array To List", "BAL", "Converts a byte array into a list of integers", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Byte Array to convert", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddIntegerParameter("Bytes", "B", "Byte Array as a list of integers", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ByteArrayGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            return;
        }

        DA.SetDataList(0, goo.Value.Select(static value => (int)value));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("60E16712-A6AA-4E02-860A-D587B512320C");
}

