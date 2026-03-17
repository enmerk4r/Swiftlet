using System.IO;
using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class SaveTextComponent : GH_Component
{
    public SaveTextComponent()
        : base("Save Text", "ST", "Save text to disk", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Content", "C", "Text to be saved to disk", GH_ParamAccess.item);
        pManager.AddTextParameter("Path", "P", "Path to file", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddIntegerParameter("Bytes", "B", "Size of saved file in bytes", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string content = string.Empty;
        string path = string.Empty;
        DA.GetData(0, ref content);
        DA.GetData(1, ref path);

        using (StreamWriter writer = new(path))
        {
            writer.Write(content);
        }

        long length = new FileInfo(path).Length;
        DA.SetData(0, length);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("1010FB58-D4A6-462D-810E-D5EE1B920FF4");
}

