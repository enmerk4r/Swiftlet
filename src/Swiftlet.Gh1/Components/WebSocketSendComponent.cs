using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class WebSocketSendComponent : GH_Component
{
    public WebSocketSendComponent()
        : base(
            "WebSocket Send",
            "WS Send",
            "Sends a message through an open WebSocket connection. Use with WebSocket Client or WebSocket Server components.",
            ShellNaming.Category,
            ShellNaming.Server)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

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
        WebSocketConnectionGoo? connectionGoo = null;
        string message = string.Empty;
        bool send = false;

        DA.GetData(0, ref connectionGoo);
        DA.GetData(1, ref message);
        DA.GetData(2, ref send);

        if (connectionGoo?.Value is null)
        {
            DA.SetData(0, false);
            DA.SetData(1, "No connection provided");
            return;
        }

        ModernWebSocketConnection connection = connectionGoo.Value;
        if (!connection.IsOpen)
        {
            DA.SetData(0, false);
            DA.SetData(1, $"Connection not open: {connection.GetStatusString()}");
            return;
        }

        if (!send)
        {
            DA.SetData(0, false);
            DA.SetData(1, "Ready to send (set Send to true)");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            DA.SetData(0, false);
            DA.SetData(1, "Message is empty");
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Message is empty");
            return;
        }

        try
        {
            bool success = connection.SendMessage(message);
            DA.SetData(0, success);
            DA.SetData(1, success ? "Message sent" : "Failed to send message");

            if (!success)
            {
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

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B");
}

