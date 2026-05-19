using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ServerResponseComponent : GH_Component
{
    public ServerResponseComponent()
        : base(
            "Server Response",
            "SR",
            "Sends an HTTP response to a pending request and closes the connection.",
            ShellNaming.Category,
            ShellNaming.Server)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new ListenerRequestParam(), "Request", "R", "The HTTP listener request from Server Input", GH_ParamAccess.item);
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Response body content", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Status", "S", "HTTP status code (default: 200)", GH_ParamAccess.item, 200);
        pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Response headers", GH_ParamAccess.list);

        pManager[1].Optional = true;
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Success", "OK", "True if the response was sent successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ListenerRequestGoo? requestGoo = null;
        RequestBodyGoo? bodyGoo = null;
        int statusCode = 200;
        List<HttpHeaderGoo> headerGoos = [];

        if (!DA.GetData(0, ref requestGoo))
        {
            DA.SetData(0, false);
            return;
        }

        DA.GetData(1, ref bodyGoo);
        DA.GetData(2, ref statusCode);
        DA.GetDataList(3, headerGoos);

        if (requestGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid request provided");
            DA.SetData(0, false);
            return;
        }

        HttpHeader[] headers = headerGoos
            .Where(static goo => goo?.Value is not null)
            .Select(static goo => goo!.Value!)
            .ToArray();

        var response = ModernServerHttpResponse.FromBody(statusCode, bodyGoo?.Value, headers);
        bool success = requestGoo.Value.TrySetResponse(response);
        if (!success)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Response has already been sent or connection closed");
        }

        DA.SetData(0, success);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("C3D4E5F6-A7B8-9012-CDEF-123456789012");
}

