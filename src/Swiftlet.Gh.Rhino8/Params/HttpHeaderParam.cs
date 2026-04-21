using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class HttpHeaderParam : GH_Param<HttpHeaderGoo>
{
    public HttpHeaderParam()
        : base("Http Header", "H", "Container for HTTP headers", ShellNaming.Category, ShellNaming.Request, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.senary;

    public override Guid ComponentGuid => new("521DAE84-7074-433E-9714-9144C42B92A4");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

