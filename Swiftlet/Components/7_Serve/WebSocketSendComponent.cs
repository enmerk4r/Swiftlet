using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;

namespace Swiftlet.Components._7_Serve
{
    /// <summary>
    /// Sends a message through an open WebSocket connection.
    /// Works with connections from both WebSocket Client and WebSocket Server components.
    /// </summary>
    public class WebSocketSendComponent : GH_Component
    {
        public WebSocketSendComponent()
          : base("WebSocket Send", "WS Send",
              "Sends a message through an open WebSocket connection.\nUse with WebSocket Client or WebSocket Server components.",
              NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new WebSocketConnectionParam(), "Connection", "C", "WebSocket connection from WebSocket Client or WebSocket Server", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "M", "Message to send", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Send", "S", "Set to true to send the message", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Success", "S", "True if the message was sent successfully", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "St", "Connection status or error message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            WebSocketConnectionGoo connectionGoo = null;
            string message = string.Empty;
            bool send = false;

            DA.GetData(0, ref connectionGoo);
            DA.GetData(1, ref message);
            DA.GetData(2, ref send);

            // Validate connection
            if (connectionGoo == null || connectionGoo.Value == null)
            {
                DA.SetData(0, false);
                DA.SetData(1, "No connection provided");
                return;
            }

            WebSocketConnection connection = connectionGoo.Value;

            // Check connection state
            if (!connection.IsOpen)
            {
                DA.SetData(0, false);
                DA.SetData(1, $"Connection not open: {connection.GetStatusString()}");
                return;
            }

            // Only send if the Send input is true
            if (!send)
            {
                DA.SetData(0, false);
                DA.SetData(1, "Ready to send (set Send to true)");
                return;
            }

            // Validate message
            if (string.IsNullOrEmpty(message))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Message is empty");
                DA.SetData(0, false);
                DA.SetData(1, "Message is empty");
                return;
            }

            // Send the message
            try
            {
                bool success = connection.SendMessage(message);

                if (success)
                {
                    DA.SetData(0, true);
                    DA.SetData(1, "Message sent");
                }
                else
                {
                    DA.SetData(0, false);
                    DA.SetData(1, "Failed to send message");
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to send message");
                }
            }
            catch (Exception ex)
            {
                DA.SetData(0, false);
                DA.SetData(1, $"Error: {ex.Message}");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return null; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B"); }
        }
    }
}
