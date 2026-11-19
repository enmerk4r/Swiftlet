using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class TextToByteArrayComponent : GH_Component
{
    public TextToByteArrayComponent()
        : base("Text To Byte Array", "TXTBA", "Converts text to a byte array", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Input Text", GH_ParamAccess.item);
        pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");
        pManager[1].Optional = false;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Byte Array", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string text = string.Empty;
        string encoding = string.Empty;
        DA.GetData(0, ref text);
        DA.GetData(1, ref encoding);

        try
        {
            DA.SetData(0, new ByteArrayGoo(UtilityEncoding.Resolve(encoding).GetBytes(text)));
        }
        catch (ArgumentException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("52C16DF2-35B5-4315-BB02-4D0C2AA1DACF");
}

