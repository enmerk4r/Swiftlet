using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateByteArrayBodyComponent : GH_Component
{
    public CreateByteArrayBodyComponent()
        : base("Create Byte Array Body", "CBAB", "Create a Request Body that supports Byte Array content", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Content", "C", "Input content (byte array, string, JSON, XML, or HTML)", GH_ParamAccess.item);
        pManager.AddTextParameter("ContentType", "T", "Content-Type header value", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        object input = null;
        string contentType = string.Empty;

        DA.GetData(0, ref input);
        DA.GetData(1, ref contentType);

        byte[] bytes = BodyInputConverter.ToLegacyBytes(input);
        if (input is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Content is null");
            return;
        }

        var body = new RequestBodyBytes(contentType, bytes);
        DA.SetData(0, new RequestBodyGoo(body));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("43F12A67-6AB6-450D-8A10-63081F20CDBF");
}

