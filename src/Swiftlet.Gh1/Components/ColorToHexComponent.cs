using Grasshopper.Kernel;
using System.Drawing;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ColorToHexComponent : GH_Component
{
    public ColorToHexComponent()
        : base("Color to Hex", "CTH", "Converts a Color value to a hex code", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddColourParameter("Color", "C", "Color to convert", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Hex", "H", "Hex code", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        Color color = default;
        if (!DA.GetData(0, ref color))
        {
            return;
        }

        DA.SetData(0, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("2E6F37C8-5C0C-4E70-9084-DCB7705871F2");
}

