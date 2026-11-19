using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class WebSocketConnectionParam : GH_Param<WebSocketConnectionGoo>
{
    public WebSocketConnectionParam()
        : base("WebSocket Connection", "WS", "An open WebSocket connection", ShellNaming.Category, ShellNaming.Server, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

