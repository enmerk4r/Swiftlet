using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateByteArrayBodyFromFileComponent : GH_Component
{
    public CreateByteArrayBodyFromFileComponent()
        : base("Create Byte Array Body from File", "BABFF", "Create a Request Body that supports Byte Array content by providing a filepath", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Path", "P", "Path to file", GH_ParamAccess.item);
        pManager.AddTextParameter("ContentType", "T", "Text contents of your request body", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string path = string.Empty;
        string contentType = string.Empty;

        DA.GetData(0, ref path);
        DA.GetData(1, ref contentType);
        byte[] content = File.ReadAllBytes(path);
        var body = new RequestBodyBytes(contentType, content);
        DA.SetData(0, new RequestBodyGoo(body));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("05DF5F52-6E60-4492-9332-03189A83EC18");
}
