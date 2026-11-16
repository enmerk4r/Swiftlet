using System.Text;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructBodyComponent : GH_Component
{
    public DeconstructBodyComponent()
        : base("Deconstruct Body", "DB", "Deconstruct a Request Body into its components", ShellNaming.Category, ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request body to deconstruct", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Content Type", "T", "The MIME content type", GH_ParamAccess.item);
        pManager.AddTextParameter("Text", "Tx", "Body content as text (UTF-8 decoded)", GH_ParamAccess.item);
        pManager.AddParameter(new ByteArrayParam(), "Bytes", "By", "Body content as byte array", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        RequestBodyGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid body provided");
            return;
        }

        byte[] bytes = goo.Value.ToByteArray();
        string text;

        try
        {
            text = Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            text = string.Empty;
        }

        DA.SetData(0, goo.Value.ContentType ?? string.Empty);
        DA.SetData(1, text);
        DA.SetData(2, new ByteArrayGoo(bytes));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("D5E6F7A8-B9C0-1234-5678-90ABCDEF1234");
}

