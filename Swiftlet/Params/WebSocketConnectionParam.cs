using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Drawing;

namespace Swiftlet.Params
{
    /// <summary>
    /// Grasshopper parameter type for WebSocket connections.
    /// </summary>
    public class WebSocketConnectionParam : GH_Param<WebSocketConnectionGoo>
    {
        public WebSocketConnectionParam()
            : base("WebSocket Connection", "WS", "An open WebSocket connection",
                 NamingUtility.CATEGORY, NamingUtility.LISTEN, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A");

        protected override Bitmap Icon => Properties.Resources.Icons_websocket_connection_param;
    }
}
