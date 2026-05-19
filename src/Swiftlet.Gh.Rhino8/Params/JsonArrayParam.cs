using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class JsonArrayParam : GH_Param<JsonArrayGoo>
{
    public JsonArrayParam()
        : base("JSON Array", "JA", "Container for JSON arrays", ShellNaming.Category, ShellNaming.ReadJson, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid => new("645A1994-D22D-4103-877E-849F96811291");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

