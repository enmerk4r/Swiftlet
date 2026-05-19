using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructMultipartFieldComponent : GH_Component
{
    public DeconstructMultipartFieldComponent()
        : base("Deconstruct Multipart Field", "DMF", "Deconstruct a multipart field into its constituent parts", ShellNaming.Category, ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new MultipartFieldParam(), "Field", "F", "Multipart field to deconstruct", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "N", "Field name", GH_ParamAccess.item);
        pManager.AddTextParameter("Text", "Tx", "Text content (if text field)", GH_ParamAccess.item);
        pManager.AddParameter(new ByteArrayParam(), "Bytes", "By", "Raw bytes", GH_ParamAccess.item);
        pManager.AddTextParameter("File Name", "Fn", "File name (if present)", GH_ParamAccess.item);
        pManager.AddTextParameter("Content Type", "T", "MIME content type", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        MultipartFieldGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid multipart field provided");
            return;
        }

        string text = goo.Value.IsText
            ? goo.Value.Text ?? string.Empty
            : string.Empty;

        DA.SetData(0, goo.Value.Name);
        DA.SetData(1, text);
        DA.SetData(2, new ByteArrayGoo(goo.Value.Bytes));
        DA.SetData(3, goo.Value.FileName ?? string.Empty);
        DA.SetData(4, goo.Value.ContentType);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("8F2EA76A-117E-40DB-A25F-466870C3C79F");
}
