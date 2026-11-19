using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ByteArrayToFileComponent : GH_Component
{
    public ByteArrayToFileComponent()
        : base("Byte Array to File", "BAF", "Save a byte array to a local file", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Byte Array to save", GH_ParamAccess.item);
        pManager.AddTextParameter("Path", "P", "Output filepath", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddIntegerParameter("Bytes", "B", "Saved file size in bytes", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ByteArrayGoo? goo = null;
        string path = string.Empty;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref path);

        if (goo?.Value is null)
        {
            return;
        }

        try
        {
            long length = FileWriteUtility.WriteBytes(path, goo.Value);
            DA.SetData(0, length);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to save file: {ex.Message}");
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("921383A8-97E2-434D-BD02-560312AD4FD8");
}

