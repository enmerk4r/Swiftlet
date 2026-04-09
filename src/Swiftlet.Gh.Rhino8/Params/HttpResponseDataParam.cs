using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class HttpResponseDataParam : GH_Param<HttpResponseDataGoo>
{
    public HttpResponseDataParam()
        : base("Http Response", "HR", "Container for HTTP responses", ShellNaming.Category, ShellNaming.Request, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.senary;

    public override Guid ComponentGuid => new("B1C8D5FD-72AD-4F80-9ADA-5F3850BC1A94");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

