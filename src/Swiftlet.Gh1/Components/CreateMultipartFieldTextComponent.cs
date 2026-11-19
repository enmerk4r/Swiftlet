using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMultipartFieldTextComponent : GH_Component
{
    public CreateMultipartFieldTextComponent()
        : base("Create Multipart Field Text", "CMFT", "Create a text multipart/form-data field", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "N", "Field name (optional)", GH_ParamAccess.item);
        pManager.AddTextParameter("Text", "T", "Field text value", GH_ParamAccess.item);
        pManager.AddTextParameter("ContentType", "C", "Field content type", GH_ParamAccess.item);

        pManager[0].Optional = true;
        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new MultipartFieldParam(), "Field", "F", "Multipart field", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string name = string.Empty;
        string text = string.Empty;
        string contentType = string.Empty;

        DA.GetData(0, ref name);
        DA.GetData(1, ref text);
        DA.GetData(2, ref contentType);

        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = ContentTypes.TextPlain;
        }

        DA.SetData(0, new MultipartFieldGoo(new MultipartField(name, text, contentType)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("7DF2CCDB-0D64-4CA4-B1B9-506B46B52338");
}

