using Grasshopper.Kernel;
using System.Drawing;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class HexToColorComponent : GH_Component
{
    public HexToColorComponent()
        : base("Hex to Color", "HTC", "Parses a hex code into a color value", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Hex", "H", "Hex color code", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddColourParameter("Color", "C", "Color represented by the hex code", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string hex = string.Empty;
        DA.GetData(0, ref hex);

        if (string.IsNullOrWhiteSpace(hex))
        {
            return;
        }

        try
        {
            string normalized = hex.StartsWith('#') ? hex : $"#{hex}";
            DA.SetData(0, ColorTranslator.FromHtml(normalized));
        }
        catch
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Unable to parse the hex code: {hex}");
            DA.SetData(0, null);
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("980E2B6E-3F59-4D1C-831B-77446E2DC407");
}

