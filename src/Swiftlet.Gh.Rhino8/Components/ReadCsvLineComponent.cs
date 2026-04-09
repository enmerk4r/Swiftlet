using Microsoft.VisualBasic.FileIO;
using System.IO;
using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ReadCsvLineComponent : GH_Component
{
    public ReadCsvLineComponent()
        : base("Read CSV Line", "RCSVL", "Extracts individual values from a delimeter-separated line", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Line", "L", "Formatted CSV line", GH_ParamAccess.item);
        pManager.AddTextParameter("Delimiter", "D", "CSV delimeter", GH_ParamAccess.item, ",");
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Cells", "C", "Cell values of a CSV line", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string line = string.Empty;
        string delimiter = ",";
        DA.GetData(0, ref line);
        DA.GetData(1, ref delimiter);

        TextFieldParser parser = new(new StringReader(line));
        if (line.Contains("\""))
        {
            parser.HasFieldsEnclosedInQuotes = true;
        }

        parser.SetDelimiters(delimiter);

        List<string> cells = new();
        while (!parser.EndOfData)
        {
            cells.AddRange(parser.ReadFields());
        }

        DA.SetDataList(0, cells);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("0478D672-BFBD-4502-B627-07B2E5D7F664");
}

