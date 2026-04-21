using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class SaveCsvComponent : GH_Component
{
    public SaveCsvComponent()
        : base("Save CSV", "CSV", "Save formatted CSV Lines as a CSV file", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Lines", "L", "CSV-formatted lines (use the \"Create CSV Line\" component)", GH_ParamAccess.list);
        pManager.AddTextParameter("Path", "P", "Output path", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddIntegerParameter("Bytes", "B", "Size of saved file in bytes", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var lines = new List<string>();
        string path = string.Empty;
        DA.GetDataList(0, lines);
        DA.GetData(1, ref path);

        try
        {
            long length = FileWriteUtility.WriteLines(path, lines);
            DA.SetData(0, length);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to save file: {ex.Message}");
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("C8F5887A-30F1-4FE5-A737-DAE37BDACC5C");
}

