using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class TextToBase64Component : GH_Component
{
    public TextToBase64Component()
        : base("Text To Base64", "TB64", "Converts text to a Base64 encoded string", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Input Text", GH_ParamAccess.item);
        pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");
        pManager[1].Optional = false;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Base64", "B", "Base64 encoded string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string text = string.Empty;
        string encoding = string.Empty;
        DA.GetData(0, ref text);
        DA.GetData(1, ref encoding);

        try
        {
            byte[] bytes = UtilityEncoding.Resolve(encoding).GetBytes(text);
            DA.SetData(0, Convert.ToBase64String(bytes));
        }
        catch (ArgumentException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("AC216550-BEA2-4F2D-B9FA-C5B0DF90C423");
}

