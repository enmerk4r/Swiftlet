using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class JsonValueParam : GH_Param<JsonValueGoo>
{
    public JsonValueParam()
        : base("JSON Value", "JV", "Container for JSON values", ShellNaming.Category, ShellNaming.ReadJson, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid => new("F46D2A50-02F2-46C9-8A93-F7A8D37843D4");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

