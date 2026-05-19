using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class ListenerRequestParam : GH_Param<ListenerRequestGoo>
{
    public ListenerRequestParam()
        : base("Listener Request", "LR", "Container for HTTP listener request contexts", ShellNaming.Category, ShellNaming.Server, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("C8EDE657-11F0-460E-9485-68B507D7668D");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

