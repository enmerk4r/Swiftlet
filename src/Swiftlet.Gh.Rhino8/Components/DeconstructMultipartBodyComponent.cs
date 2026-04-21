using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructMultipartBodyComponent : GH_Component
{
    public DeconstructMultipartBodyComponent()
        : base("Deconstruct Multipart Body", "DMB", "Deconstruct a multipart/form-data body into individual fields", ShellNaming.Category, ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "A multipart/form-data request body", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new MultipartFieldParam(), "Fields", "F", "Individual multipart fields", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        RequestBodyGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid body provided");
            return;
        }

        List<MultipartFieldGoo> fields;

        if (goo.Value is RequestBodyMultipartForm multipartBody)
        {
            fields = multipartBody.Fields
                .Select(static field => new MultipartFieldGoo(field))
                .ToList();
        }
        else if (goo.Value.ContentType is not null
            && goo.Value.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            fields = ParseMultipartBytes(goo.Value.ToByteArray(), goo.Value.ContentType);
        }
        else
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Body is not multipart/form-data");
            return;
        }

        DA.SetDataList(0, fields);
    }

    private List<MultipartFieldGoo> ParseMultipartBytes(byte[] bytes, string contentType)
    {
        try
        {
            return MultipartBodyParser.Parse(bytes, contentType)
                .Select(static field => new MultipartFieldGoo(field))
                .ToList();
        }
        catch (FormatException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to parse multipart body: {ex.Message}");
        }

        return [];
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("F38C3EC8-A611-46C3-8C79-A1B38DE62D00");
}
