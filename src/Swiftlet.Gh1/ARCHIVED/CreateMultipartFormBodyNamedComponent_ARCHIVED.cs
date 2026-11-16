using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

[Obsolete("Archived legacy component. Use Create Multipart Form Body with Multipart Field components instead.")]
public sealed class CreateMultipartFormBodyNamedComponent_ARCHIVED : GH_Component
{
    public CreateMultipartFormBodyNamedComponent_ARCHIVED()
        : base(
            "Create Multipart Form Body Named",
            "CMFBN",
            "[DEPRECATED] Create a Request Body that supports the multipart/form-data Content-Type with named fields",
            ShellNaming.Category,
            ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Keys", "K", "Names of Multipart Form fields", GH_ParamAccess.list);
        pManager.AddParameter(new RequestBodyParam(), "Fields", "F", "Multipart form fields", GH_ParamAccess.list);
        pManager[0].Optional = true;
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<string> keys = [];
        List<RequestBodyGoo> fields = [];

        DA.GetDataList(0, keys);
        DA.GetDataList(1, fields);

        if (keys.Count != fields.Count)
        {
            throw new Exception("The number of Keys must match the number of Fields");
        }

        IRequestBody[] values = fields
            .Select(static field => field?.Value ?? throw new Exception("Fields must be valid request bodies"))
            .ToArray();

        DA.SetData(0, new RequestBodyGoo(new RequestBodyMultipartForm(keys, values)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("45321F4D-BB8E-4844-8B18-F663E3A95896");
}
