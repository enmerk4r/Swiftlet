using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructHttpResponseComponent : GH_Component
{
    public DeconstructHttpResponseComponent()
        : base("Deconstruct Response", "DR", "Deconstruct Http Response", ShellNaming.Category, ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HttpResponseDataParam(), "Response", "R", "Http Web response to deconstruct", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Version", "V", "The HTTP message version. The default is 1.1", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Status", "S", "Http status code", GH_ParamAccess.item);
        pManager.AddTextParameter("Reason", "R", "The reason phrase which typically is sent by servers together with the status code", GH_ParamAccess.item);
        pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "The collection of HTTP response headers", GH_ParamAccess.list);
        pManager.AddBooleanParameter("IsSuccess", "iS", "Indicates if the HTTP response was successful", GH_ParamAccess.item);
        pManager.AddTextParameter("Content", "C", "Response content", GH_ParamAccess.item);
        pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Response data as byte array", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HttpResponseDataGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            return;
        }

        DA.SetData(0, goo.Value.Version);
        DA.SetData(1, goo.Value.StatusCode);
        DA.SetData(2, goo.Value.ReasonPhrase);
        DA.SetDataList(3, goo.Value.Headers.Select(static header => new HttpHeaderGoo(header)));
        DA.SetData(4, goo.Value.IsSuccessStatusCode);
        DA.SetData(5, goo.Value.Content);
        DA.SetData(6, new ByteArrayGoo(goo.Value.Bytes));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("6b60cc1e-e463-4997-b1b3-7d1835ec0c0b");
}

