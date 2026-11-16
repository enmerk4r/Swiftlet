using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class Base64ToTextComponent : GH_Component
{
    public Base64ToTextComponent()
        : base("Base64 to Text", "B64T", "Converts a Base64 encoded string to cleartext", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Base64", "B", "Base64 encoded string", GH_ParamAccess.item);
        pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");
        pManager[1].Optional = false;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Converted Text", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string text = string.Empty;
        string encoding = string.Empty;
        DA.GetData(0, ref text);
        DA.GetData(1, ref encoding);

        try
        {
            byte[] bytes = Convert.FromBase64String(text);
            DA.SetData(0, UtilityEncoding.Resolve(encoding).GetString(bytes));
        }
        catch (ArgumentException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("2385C560-3F23-4196-BB17-5A0C8F7B4ACA");
}

