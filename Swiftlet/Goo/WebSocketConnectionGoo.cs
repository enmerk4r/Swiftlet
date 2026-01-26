using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;
using System;

namespace Swiftlet.Goo
{
    /// <summary>
    /// Grasshopper Goo wrapper for WebSocketConnection.
    /// Allows WebSocket connections to flow through Grasshopper's data system.
    /// </summary>
    public class WebSocketConnectionGoo : GH_Goo<WebSocketConnection>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "WebSocket Connection";

        public override string TypeDescription => "An open WebSocket connection that can be used to send and receive messages";

        public WebSocketConnectionGoo()
        {
            this.Value = null;
        }

        public WebSocketConnectionGoo(WebSocketConnection connection)
        {
            this.Value = connection;
        }

        public override IGH_Goo Duplicate()
        {
            // Return the same reference - we want all downstream components
            // to share the same connection context
            return new WebSocketConnectionGoo(this.Value);
        }

        public override string ToString()
        {
            if (this.Value == null)
                return "No WebSocket Connection";

            return this.Value.ToString();
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(WebSocketConnection)))
            {
                target = (Q)(object)this.Value;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(string)))
            {
                target = (Q)(object)this.ToString();
                return true;
            }

            return false;
        }

        public override bool CastFrom(object source)
        {
            if (source == null) return false;

            if (source is WebSocketConnection conn)
            {
                this.Value = conn;
                return true;
            }

            if (source is WebSocketConnectionGoo goo)
            {
                this.Value = goo.Value;
                return true;
            }

            return false;
        }
    }
}
