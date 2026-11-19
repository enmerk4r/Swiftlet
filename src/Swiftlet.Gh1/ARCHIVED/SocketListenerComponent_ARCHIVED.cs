using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

[Obsolete("Archived legacy component. Use WebSocket Client instead.")]
public sealed class SocketListenerComponent_ARCHIVED : GH_Component
{
    private readonly ModernWebSocketClientSession _session = new();
    private string? _currentUrl;
    private List<string> _currentOnOpen = [];
    private bool _freeze;
    private string? _lastMessage;
    private int _failCounter;
    private int _updateScheduled;

    public SocketListenerComponent_ARCHIVED()
        : base(
            "Socket Listener",
            "SOCKET",
            "[DEPRECATED - Use WebSocket Client instead]\nA simple socket listener component.",
            ShellNaming.Category,
            ShellNaming.Server)
    {
        _session.StateChanged += OnStateChanged;
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URL", "U", "URL of WebSocket resource", GH_ParamAccess.item);
        pManager.AddParameter(new QueryParameterParam(), "Params", "P", "Query Params", GH_ParamAccess.list);
        pManager.AddTextParameter("On Open", "O", "Messages to be sent to the WebSocket server after opening the connection", GH_ParamAccess.list);
        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Messages", "M", "Inbound Socket Messages", GH_ParamAccess.list);
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
        List<string> onOpen = [];

        DA.GetData(0, ref url);
        DA.GetDataList(1, queryGoos);
        DA.GetDataList(2, onOpen);

        if (string.IsNullOrWhiteSpace(url))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Url");
            DA.SetData(0, _lastMessage);
            return;
        }

        QueryParameter[] parameters = queryGoos
            .Where(static goo => goo?.Value is not null)
            .Select(static goo => goo!.Value!)
            .ToArray();

        string fullUrl = UrlBuilder.AddQueryParameters(url, parameters);
        bool inputsChanged = InputsChanged(fullUrl, onOpen);

        if (!_session.IsConnected || inputsChanged)
        {
            Reconnect(url, parameters, fullUrl, onOpen);
        }

        while (_session.TryDequeueMessage(out string? message) && !string.IsNullOrEmpty(message))
        {
            _lastMessage = message;
        }

        if (_session.LastError is not null)
        {
            Message = "Error";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _session.LastError);
        }
        else
        {
            Message = _session.Connection?.GetStatusString() ?? "Disconnected";
        }

        DA.SetData(0, _lastMessage);
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

    public override Guid ComponentGuid => new("2BAEA8DD-9496-4644-9077-67EF4910675E");

    private void Reconnect(string url, QueryParameter[] parameters, string fullUrl, List<string> onOpen)
    {
        if (_failCounter >= 5)
        {
            _failCounter = 0;
            return;
        }

        try
        {
            ClearRuntimeMessages();
            _lastMessage = null;
            _session.ReconnectAsync(url, parameters).GetAwaiter().GetResult();
            _currentUrl = fullUrl;
            _currentOnOpen = [.. onOpen];
            _failCounter = 0;

            foreach (string message in onOpen)
            {
                _session.Connection?.SendMessage(message);
            }
        }
        catch (Exception ex)
        {
            _failCounter++;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    private bool InputsChanged(string fullUrl, List<string> onOpen)
    {
        if (!string.Equals(_currentUrl, fullUrl, StringComparison.Ordinal))
        {
            return true;
        }

        if (_currentOnOpen.Count != onOpen.Count)
        {
            return true;
        }

        for (int index = 0; index < onOpen.Count; index++)
        {
            if (!string.Equals(_currentOnOpen[index], onOpen[index], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

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
