using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class WebSocketClientComponent : GH_Component
{
    private readonly ModernWebSocketClientSession _session = new();
    private string? _currentUrl;
    private bool _run;
    private bool _freeze;
    private string? _lastMessage;

    public WebSocketClientComponent()
        : base(
            "WebSocket Client",
            "WS Client",
            "Connects to a WebSocket server and receives messages.\nOutputs a Connection that can be used with WebSocket Send to send messages.",
            ShellNaming.Category,
            ShellNaming.Server)
    {
        _session.StateChanged += OnStateChanged;
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URL", "U", "URL of WebSocket server (ws:// or wss://)", GH_ParamAccess.item);
        pManager.AddParameter(new QueryParameterParam(), "Params", "P", "Query parameters to append to the URL", GH_ParamAccess.list);
        pManager.AddBooleanParameter("Run", "R", "Set to true to connect, false to disconnect", GH_ParamAccess.item, false);
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new WebSocketConnectionParam(), "Connection", "C", "WebSocket connection for sending messages via WebSocket Send component", GH_ParamAccess.item);
        pManager.AddTextParameter("Message", "M", "Most recent message received from the server", GH_ParamAccess.item);
        pManager.AddTextParameter("Messages", "Ms", "All messages received since last solve", GH_ParamAccess.list);
        pManager.AddTextParameter("Status", "S", "Connection status", GH_ParamAccess.item);
    }

    protected override void BeforeSolveInstance()
    {
        _freeze = true;
        base.BeforeSolveInstance();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string url = string.Empty;
        List<QueryParameterGoo> queryGoos = [];
        bool run = false;

        DA.GetData(0, ref url);
        DA.GetDataList(1, queryGoos);
        DA.GetData(2, ref run);

        if (string.IsNullOrWhiteSpace(url))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "URL cannot be empty");
            DA.SetData(3, "Error: No URL");
            return;
        }

        if (!url.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "URL must start with ws:// or wss://");
            DA.SetData(3, "Error: Invalid URL scheme (use ws:// or wss://)");
            return;
        }

        QueryParameter[] parameters = queryGoos
            .Where(static goo => goo?.Value is not null)
            .Select(static goo => goo!.Value!)
            .ToArray();

        string fullUrl = UrlBuilder.AddQueryParameters(url, parameters);

        try
        {
            if (run && !_run)
            {
                _lastMessage = null;
                _session.ReconnectAsync(url, parameters).GetAwaiter().GetResult();
                _currentUrl = fullUrl;
            }
            else if (!run && _run)
            {
                _session.DisconnectAsync().GetAwaiter().GetResult();
                _currentUrl = null;
                _lastMessage = null;
            }
            else if (run && !string.Equals(_currentUrl, fullUrl, StringComparison.Ordinal) && _session.IsConnected)
            {
                _lastMessage = null;
                _session.ReconnectAsync(url, parameters).GetAwaiter().GetResult();
                _currentUrl = fullUrl;
            }
            else if (run && !_session.IsConnected)
            {
                _session.ReconnectAsync(url, parameters).GetAwaiter().GetResult();
                _currentUrl = fullUrl;
            }
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }

        _run = run;

        List<string> messages = [];
        while (_session.TryDequeueMessage(out string? message) && !string.IsNullOrEmpty(message))
        {
            messages.Add(message);
            _lastMessage = message;
        }

        if (_session.Connection is not null)
        {
            DA.SetData(0, new WebSocketConnectionGoo(_session.Connection));
        }

        string status = _session.LastError is not null
            ? $"Error: {_session.LastError}"
            : _session.Connection?.GetStatusString() ?? "Disconnected";

        Message = _session.Connection?.GetStatusString() ?? (_session.LastError is null ? "Disconnected" : "Error");

        DA.SetData(1, _lastMessage);
        DA.SetDataList(2, messages);
        DA.SetData(3, status);
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

    public override Guid ComponentGuid => new("A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D");

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_freeze)
        {
            return;
        }

        Rhino.RhinoApp.InvokeOnUiThread((Action)(() => ExpireSolution(true)));
    }
}

