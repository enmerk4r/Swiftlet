using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class WebSocketServerComponent : GH_Component
{
    private readonly ModernWebSocketServerSession _session = new();
    private string? _lastMessage;
    private ModernWebSocketConnection? _lastConnection;
    private bool _freeze;
    private int _updateScheduled;

    public WebSocketServerComponent()
        : base(
            "WebSocket Server",
            "WS Server",
            "Accepts WebSocket connections from clients. Outputs Connection for each message received, use with WebSocket Send to respond.",
            ShellNaming.Category,
            ShellNaming.Server)
    {
        _session.StateChanged += OnStateChanged;
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Port", "P", "Port number to listen on (0-65535)", GH_ParamAccess.item, 8181);
        pManager.AddBooleanParameter("Run", "R", "Set to true to start the server, false to stop", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new WebSocketConnectionParam(), "Connection", "C", "Connection context for the client that sent the message (use with WebSocket Send)", GH_ParamAccess.item);
        pManager.AddTextParameter("Message", "M", "Most recent message received", GH_ParamAccess.item);
        pManager.AddTextParameter("Messages", "Ms", "All messages received since last solve", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Clients", "N", "Number of connected clients", GH_ParamAccess.item);
        pManager.AddTextParameter("Status", "S", "Server status", GH_ParamAccess.item);
    }

    protected override void BeforeSolveInstance()
    {
        _freeze = true;
        base.BeforeSolveInstance();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int port = 8181;
        bool run = false;

        DA.GetData(0, ref port);
        DA.GetData(1, ref run);

        if (port < 0 || port > 65535)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port must be between 0 and 65535");
            DA.SetData(4, "Error: Invalid port");
            return;
        }

        try
        {
            if (run)
            {
                _session.ReconfigureAsync(port).GetAwaiter().GetResult();
            }
            else if (_session.IsRunning)
            {
                _session.StopAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start server: {ex.Message}");
            DA.SetData(4, $"Error: {ex.Message}");
            return;
        }

        List<string> messages = [];
        while (_session.TryDequeueMessage(out ModernWebSocketReceivedMessage? message) && message is not null)
        {
            messages.Add(message.Message);
            _lastMessage = message.Message;
            _lastConnection = message.Connection;
        }

        if (_lastConnection is null || !_lastConnection.IsOpen)
        {
            _session.TryGetAnyOpenConnection(out _lastConnection);
        }

        string status = run ? $"Listening on ws://localhost:{port}/" : "Stopped";
        Message = run ? $"ws://localhost:{port}/ ({_session.ActiveClientCount} clients)" : "Stopped";

        if (_lastConnection is not null)
        {
            DA.SetData(0, new WebSocketConnectionGoo(_lastConnection));
        }

        DA.SetData(1, _lastMessage);
        DA.SetDataList(2, messages);
        DA.SetData(3, _session.ActiveClientCount);
        DA.SetData(4, status);
    }

    protected override void AfterSolveInstance()
    {
        _freeze = false;
        base.AfterSolveInstance();
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        _session.StateChanged -= OnStateChanged;
        _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.DocumentContextChanged(document, context);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C");

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_freeze)
        {
            return;
        }

        ScheduleComponentUpdate();
    }

    private void ScheduleComponentUpdate()
    {
        if (Interlocked.Exchange(ref _updateScheduled, 1) == 1)
        {
            return;
        }

        Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            GH_Document? document = OnPingDocument();
            if (document is null)
            {
                Interlocked.Exchange(ref _updateScheduled, 0);
                return;
            }

            document.ScheduleSolution(5, _ =>
            {
                Interlocked.Exchange(ref _updateScheduled, 0);
                ExpireSolution(false);
            });
        }));
    }
}

