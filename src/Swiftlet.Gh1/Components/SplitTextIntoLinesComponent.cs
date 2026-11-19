using System.Linq;
using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class SplitTextIntoLinesComponent : GH_Component
{
    public SplitTextIntoLinesComponent()
        : base("Split Text Into Lines", "STIL", "Takes a block of text and splits it into individual lines by the newline character", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Text to split into lines", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Lines", "L", "Individual lines", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string text = string.Empty;
        DA.GetData(0, ref text);

        DA.SetDataList(0, text.Split('\n').ToList());
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("3561EF51-707A-414B-B344-68B79108BC46");
}

