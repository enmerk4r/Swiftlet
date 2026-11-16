using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateCsvLineComponent : GH_Component
{
    public CreateCsvLineComponent()
        : base("Create CSV Line", "CSVL", "Formats multiple strings as a single line of delimeter-separated values", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Cells", "C", "Cell values of a CSV line", GH_ParamAccess.list);
        pManager.AddTextParameter("Delimiter", "D", "CSV delimeter", GH_ParamAccess.item, ",");
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Line", "L", "Formatted CSV line", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var cells = new List<string>();
        string delimiter = ",";
        DA.GetDataList(0, cells);
        DA.GetData(1, ref delimiter);

        string line = string.Empty;
        foreach (string cell in cells)
        {
            string cleanCell = cell;
            if (cell.Contains(delimiter))
            {
                cleanCell = $"\"{cell}\"";
            }

            line += $"{cleanCell}{delimiter}";
        }

        line = line.Remove(line.Length - 1, 1);
        DA.SetData(0, line);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("D9B2041E-FF4E-446E-B634-4EBACD47051E");
}

