using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class RequestBodyParam : GH_Param<RequestBodyGoo>
{
    public RequestBodyParam()
        : base("Request Body", "RB", "Container for request bodies", ShellNaming.Category, ShellNaming.Request, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.senary;

    public override Guid ComponentGuid => new("CC4B9260-48DC-432E-80C8-5EBF9AB3F66E");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

