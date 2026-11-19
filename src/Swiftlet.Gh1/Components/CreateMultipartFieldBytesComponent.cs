using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMultipartFieldBytesComponent : GH_Component
{
    public CreateMultipartFieldBytesComponent()
        : base("Create Multipart Field Bytes", "CMFBy", "Create a byte multipart/form-data field", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "N", "Field name (optional)", GH_ParamAccess.item);
        pManager.AddParameter(new ByteArrayParam(), "Bytes", "B", "Field bytes", GH_ParamAccess.item);
        pManager.AddTextParameter("FileName", "F", "Optional filename", GH_ParamAccess.item);
        pManager.AddTextParameter("ContentType", "C", "Field content type", GH_ParamAccess.item);

        pManager[0].Optional = true;
        pManager[1].Optional = true;
        pManager[2].Optional = true;
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new MultipartFieldParam(), "Field", "F", "Multipart field", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string name = string.Empty;
        ByteArrayGoo? bytesGoo = null;
        string fileName = string.Empty;
        string contentType = string.Empty;

        DA.GetData(0, ref name);
        DA.GetData(1, ref bytesGoo);
        DA.GetData(2, ref fileName);
        DA.GetData(3, ref contentType);

        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = ContentTypes.ApplicationOctetStream;
        }

        byte[] bytes = bytesGoo?.Value ?? Array.Empty<byte>();
        DA.SetData(0, new MultipartFieldGoo(new MultipartField(name, bytes, fileName, contentType)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("1A6521B4-40DA-40AD-BC43-9B067A9EA6A8");
}

