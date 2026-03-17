using System.IO;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class FileToByteArrayComponent : GH_Component
{
    public FileToByteArrayComponent()
        : base("File To Byte Array", "FBA", "Read a file into a byte array", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Path", "P", "Input File", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Byte Array", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string path = string.Empty;
        DA.GetData(0, ref path);
        DA.SetData(0, new ByteArrayGoo(File.ReadAllBytes(path)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("6514B6D2-779E-4CD6-92B7-91A2F4A36360");
}

