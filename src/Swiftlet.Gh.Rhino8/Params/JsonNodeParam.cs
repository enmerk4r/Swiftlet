using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class JsonNodeParam : GH_Param<JsonNodeGoo>
{
    public JsonNodeParam()
        : base("JSON Token", "JT", "Container for JSON tokens", ShellNaming.Category, ShellNaming.ReadJson, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid => new("A2BDB76C-7A0E-4537-A7F1-0FB37A6B35AC");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

