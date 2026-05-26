using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

[Obsolete("Archived legacy component. Use Create Multipart Form Body with Multipart Field components instead.")]
public sealed class CreateMultipartFormBodyUnnamedComponent_ARCHIVED : GH_Component
{
    public CreateMultipartFormBodyUnnamedComponent_ARCHIVED()
        : base(
            "Create Multipart Form Body Unnamed",
            "CMFBU",
            "[DEPRECATED] Create a Request Body that supports the multipart/form-data Content-Type with unnamed fields",
            ShellNaming.Category,
            ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Fields", "F", "Multipart form fields", GH_ParamAccess.list);
        pManager[0].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<RequestBodyGoo> fields = [];

        DA.GetDataList(0, fields);

        IRequestBody[] values = fields
            .Select(static field => field?.Value ?? throw new Exception("Fields must be valid request bodies"))
            .ToArray();

        DA.SetData(0, new RequestBodyGoo(new RequestBodyMultipartForm(values)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("BFE253AA-AB8A-4424-872A-37F5CB1E7096");
}
