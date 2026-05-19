using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class QueryParameterParam : GH_Param<QueryParameterGoo>
{
    public QueryParameterParam()
        : base("Query Param", "QP", "Container for HTTP query parameters", ShellNaming.Category, ShellNaming.Request, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.senary;

    public override Guid ComponentGuid => new("F59B2397-A996-48B3-8AA3-6B8D56F6244F");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

