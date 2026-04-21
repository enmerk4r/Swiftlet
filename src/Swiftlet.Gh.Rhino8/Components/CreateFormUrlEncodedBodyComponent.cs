using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateFormUrlEncodedBodyComponent : GH_Component
{
    public CreateFormUrlEncodedBodyComponent()
        : base(
            "Create Form URL Encoded Body",
            "CFUEB",
            "Create a request body with application/x-www-form-urlencoded content type. Commonly used for HTML form submissions and OAuth token endpoints.",
            ShellNaming.Category,
            ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Keys", "K", "Form field names", GH_ParamAccess.list);
        pManager.AddTextParameter("Values", "V", "Form field values", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Form URL encoded request body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<string> keys = [];
        List<string> values = [];

        DA.GetDataList(0, keys);
        DA.GetDataList(1, values);

        if (keys.Count != values.Count)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Error,
                $"Keys and values must have the same count. Got {keys.Count} keys and {values.Count} values.");
            return;
        }

        if (keys.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No form fields provided");
        }

        var body = new RequestBodyFormUrlEncoded(keys, values);
        DA.SetData(0, new RequestBodyGoo(body));
        Message = "x-www-form-urlencoded";
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("C9D0E1F2-A3B4-2233-7E8F-90011223344A");
}

