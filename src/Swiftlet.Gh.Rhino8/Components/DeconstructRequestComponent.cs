using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructRequestComponent : GH_Component
{
    public DeconstructRequestComponent()
        : base(
            "Deconstruct Request",
            "DeReq",
            "Extracts data from an HTTP request context.",
            ShellNaming.Category,
            ShellNaming.Server)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new ListenerRequestParam(), "Request", "R", "The HTTP listener request from Server Input", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ListenerRequestParam(), "Request", "R", "Pass-through request for Server Response", GH_ParamAccess.item);
        pManager.AddTextParameter("Method", "M", "HTTP method (GET, POST, etc.)", GH_ParamAccess.item);
        pManager.AddTextParameter("Route", "Rt", "Request path", GH_ParamAccess.item);
        pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Request headers", GH_ParamAccess.list);
        pManager.AddParameter(new QueryParameterParam(), "Query", "Q", "Query string parameters", GH_ParamAccess.list);
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ListenerRequestGoo? requestGoo = null;
        if (!DA.GetData(0, ref requestGoo))
        {
            return;
        }

        if (requestGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid request provided");
            return;
        }

        ModernServerRequestContext request = requestGoo.Value;

        DA.SetData(0, new ListenerRequestGoo(request));
        DA.SetData(1, request.Method);
        DA.SetData(2, request.Path);
        DA.SetDataList(3, request.Headers.Select(static header => new HttpHeaderGoo(header)));
        DA.SetDataList(4, request.QueryParameters.Select(static parameter => new QueryParameterGoo(parameter)));
        DA.SetData(5, new RequestBodyGoo(request.Body));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B2C3D4E5-F6A7-8901-BCDE-F12345678901");
}

