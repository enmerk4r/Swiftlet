using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GenerateGuidComponent : GH_Component
{
    public GenerateGuidComponent()
        : base("Generate GUID", "GGUID", "Generates a random GUID (Globally Unique Identifier)\nThis is useful if you need a quick way to reliably generate unique IDs", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.senary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddBooleanParameter("Generate", "G", "Generate a GUID!", GH_ParamAccess.item, true);
        pManager.AddTextParameter(
            "Format",
            "F",
            "Format of the GUID as a single letter:\n" +
            "\nN - 32 digits:" +
            "\n00000000000000000000000000000000\n" +
            "\nD - 32 digits separated by hyphens:" +
            "\n00000000-0000-0000-0000-000000000000\n" +
            "\nB - 32 digits separated by hyphens, enclosed in braces:" +
            "\n{00000000-0000-0000-0000-000000000000}\n" +
            "\nP - 32 digits separated by hyphens, enclosed in parentheses:" +
            "\n(00000000-0000-0000-0000-000000000000)\n" +
            "\nX - Four hexadecimal values enclosed in braces, where the fourth value is a subset of eight hexadecimal values that is also enclosed in braces:" +
            "\n{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}\n",
            GH_ParamAccess.item,
            "D");
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("GUID", "G", "Random GUID", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        bool generate = true;
        string format = string.Empty;
        DA.GetData(0, ref generate);
        DA.GetData(1, ref format);

        if (generate)
        {
            DA.SetData(0, Guid.NewGuid().ToString(format));
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("E4CD167A-1D5B-4C43-A1DB-70DA520DBDE7");
}

