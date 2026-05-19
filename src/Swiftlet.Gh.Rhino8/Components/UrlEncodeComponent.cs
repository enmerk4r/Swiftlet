using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class UrlEncodeComponent : GH_Component
{
    public UrlEncodeComponent()
        : base("URL encode", "URLE", "URL-encodes a string to make it URL-safe", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Text to be URL encoded", GH_ParamAccess.item);
        pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Encoded", "E", "URL-encoded string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string text = string.Empty;
        string encoding = "UTF8";
        DA.GetData(0, ref text);
        DA.GetData(1, ref encoding);

        try
        {
            DA.SetData(0, UtilityUrlEncoding.Encode(text, UtilityEncoding.Resolve(encoding)));
        }
        catch (ArgumentException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("9D385C97-7975-43A3-9AE7-2FABBE5BD4C8");
}

