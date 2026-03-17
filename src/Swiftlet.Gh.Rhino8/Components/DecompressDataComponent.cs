using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DecompressDataComponent : GH_Component
{
    public DecompressDataComponent()
        : base("Decompress Data", "UGZIP", "Un-GZIP byte array data", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Compressed", "C", "Compressed text", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Uncompressed", "U", "Uncompressed text", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ByteArrayGoo? input = null;
        if (!DA.GetData(0, ref input) || input?.Value is null)
        {
            return;
        }

        DA.SetData(0, new ByteArrayGoo(UtilityCompression.Decompress(input.Value)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("4DA74EEA-0134-4AB4-8E68-7E929E839BBE");
}

