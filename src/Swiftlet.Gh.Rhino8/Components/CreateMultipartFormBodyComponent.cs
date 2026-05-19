using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMultipartFormBodyComponent : GH_Component
{
    public CreateMultipartFormBodyComponent()
        : base("Create Multipart Form Body", "CMFB", "Create a Request Body that supports multipart/form-data", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new MultipartFieldParam(), "Fields", "F", "Multipart form fields", GH_ParamAccess.list);
        pManager[0].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<MultipartFieldGoo> fields = [];
        DA.GetDataList(0, fields);

        MultipartField[] values = fields
            .Where(static field => field?.Value is not null)
            .Select(static field => field!.Value!)
            .ToArray();

        DA.SetData(0, new RequestBodyGoo(new RequestBodyMultipartForm(values)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("15C68C1C-9E2C-4DF3-83F3-8D9FB1FCD5CF");
}

