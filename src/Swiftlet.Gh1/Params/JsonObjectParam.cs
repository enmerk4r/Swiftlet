using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class JsonObjectParam : GH_Param<JsonObjectGoo>
{
    public JsonObjectParam()
        : base("JSON Object", "JO", "Container for JSON objects", ShellNaming.Category, ShellNaming.ReadJson, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid => new("90F38432-A460-43E2-A1AC-747D8CA6236C");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

