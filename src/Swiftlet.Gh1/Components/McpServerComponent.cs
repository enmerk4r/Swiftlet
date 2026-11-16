using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed partial class McpServerComponent : GH_Component, IGH_VariableParameterComponent
{
    private readonly ModernMcpServerSession _session = new();
    private readonly RhinoHostServices _hostServices = new();
    private IReadOnlyList<McpToolDefinition> _tools = [];
    private readonly McpCallRequestState _calls = new();
    private int _stopping;
    private long _generation;
    private int _updateScheduled;

    public McpServerComponent()
        : base("MCP Server", "MCP", "Starts an MCP server and exposes the connected tools to MCP clients. Each connected tool appears as its own output and emits a request when that tool is called.", ShellNaming.Category, ShellNaming.Mcp)
    {
        _session.RequestQueued += OnRequestQueued;
        _calls.RequestReady += OnRequestQueued;
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Port", "P", "Local TCP port for the MCP server. Use this port when connecting an MCP client to Swiftlet. Default: 3001.", GH_ParamAccess.item, 3001);
        pManager.AddParameter(new McpToolDefinitionParam(), "Tools", "T", "Tool definitions to expose to the MCP client. Each connected tool creates a matching request output on this component.", GH_ParamAccess.list);
        pManager.AddTextParameter("Server Name", "N", "Server name reported to the MCP client in the MCP connection info.", GH_ParamAccess.item, "Swiftlet");
        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int port = 3001;
        List<McpToolDefinitionGoo> toolGoos = [];
        string serverName = "Swiftlet";

        DA.GetData(0, ref port);
        DA.GetDataList(1, toolGoos);
        DA.GetData(2, ref serverName);

        if (port < 1 || port > 65535)
        {
            _calls.Clear();
            _session.StopAsync().GetAwaiter().GetResult();
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port number must be between 1 and 65535");
            return;
        }

        if (Volatile.Read(ref _stopping) != 0 || Locked) return;

        _tools = toolGoos
            .Where(static goo => goo?.Value is not null)
            .Select(static goo => goo!.Value!)
            .ToList();

        if (NeedsOutputUpdate())
        {
            ScheduleOutputUpdate();
            return;
        }

        try
        {
            if (_session.Port != port) _calls.Clear();
            _session.ReconfigureAsync(port, serverName, _tools).GetAwaiter().GetResult();
            Message = _session.StatusMessage ?? string.Empty;
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start MCP server: {ex.Message}");
            Message = "Error";
            return;
        }

        var calls = _calls.Capture(Params.Output.Select(output => output.NickName),
            name => _session.TryDequeuePendingCall(name, out var call) ? call : null);
        for (int index = 0; index < calls.Length; index++)
            DA.SetData(index, calls[index] is { } call ? new McpToolCallRequestGoo(call) : null);

    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => false;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
        return new Param_GenericObject();
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => true;

    public void VariableParameterMaintenance()
    {
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        Volatile.Write(ref _stopping, 0);
        _session.RequestQueued -= OnRequestQueued;
        _session.RequestQueued += OnRequestQueued;
        _calls.RequestReady -= OnRequestQueued;
        _calls.RequestReady += OnRequestQueued;
    }

    private void StopServer()
    {
        Volatile.Write(ref _stopping, 1);
        Interlocked.Increment(ref _generation);
        Interlocked.Exchange(ref _updateScheduled, 0);
        _calls.Clear();
        _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        _session.RequestQueued -= OnRequestQueued;
        _calls.RequestReady -= OnRequestQueued;
        StopServer();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            StopServer();
        }

        base.DocumentContextChanged(document, context);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("D4E5F6A7-B8C9-0123-DEF0-234567890123");

    private bool NeedsOutputUpdate()
    {
        HashSet<string> currentOutputs = Params.Output.Select(static output => output.NickName).ToHashSet(StringComparer.Ordinal);
        HashSet<string> desiredOutputs = GetDesiredOutputNames();
        return !currentOutputs.SetEquals(desiredOutputs);
    }

    private void ScheduleOutputUpdate()
    {
        GH_Document? document = OnPingDocument();
        long generation = Volatile.Read(ref _generation);
        document?.ScheduleSolution(5, _ => { if (generation == Volatile.Read(ref _generation) && Volatile.Read(ref _stopping) == 0) UpdateOutputsFromTools(); });
    }

    private void UpdateOutputsFromTools()
    {
        HashSet<string> desiredOutputs = GetDesiredOutputNames();
        bool changed = false;

        for (int i = Params.Output.Count - 1; i >= 0; i--)
        {
            if (!desiredOutputs.Contains(Params.Output[i].NickName))
            {
                Params.UnregisterOutputParameter(Params.Output[i]);
                changed = true;
            }
        }

        HashSet<string> currentOutputs = Params.Output.Select(static output => output.NickName).ToHashSet(StringComparer.Ordinal);
        foreach (McpToolDefinition tool in _tools)
        {
            if (currentOutputs.Contains(tool.Name))
            {
                continue;
            }

            var param = new McpToolCallRequestParam
            {
                Name = tool.Name,
                NickName = tool.Name,
                Description = $"Emits a request when the MCP client calls '{tool.Name}'. Connect it to Deconstruct Tool Call, process the request, then finish with MCP Tool Response.",
                Access = GH_ParamAccess.item,
            };

            Params.RegisterOutputParam(param);
            changed = true;
        }

        if (changed)
        {
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
    }

    private void OnRequestQueued(object? sender, EventArgs e)
    {
        ScheduleComponentUpdate();
    }

    private void ScheduleComponentUpdate()
    {
        long generation = Volatile.Read(ref _generation);
        if (Volatile.Read(ref _stopping) != 0 || Interlocked.Exchange(ref _updateScheduled, 1) == 1)
        {
            return;
        }

        Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            if (generation != Volatile.Read(ref _generation) || Volatile.Read(ref _stopping) != 0) return;
            GH_Document? document = OnPingDocument();
            if (document is null)
            {
                Interlocked.Exchange(ref _updateScheduled, 0);
                return;
            }

            document.ScheduleSolution(5, _ =>
            {
                Interlocked.Exchange(ref _updateScheduled, 0);
                if (generation == Volatile.Read(ref _generation) && Volatile.Read(ref _stopping) == 0 && !Locked) ExpireSolution(false);
            });
        }));
    }

    private bool TryBuildConfig(McpClientConfigTarget target, out string? config, out string? error)
    {
        try
        {
            config = _session.GenerateConfig(GetType().Assembly.Location, target);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            config = null;
            error = $"Failed to generate MCP config: {ex.Message}";
            return false;
        }
    }

    private bool CopyConfigToClipboard(McpClientConfigTarget target)
    {
        if (!TryBuildConfig(target, out string? config, out string? configError) || string.IsNullOrWhiteSpace(config))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, configError ?? "Failed to generate MCP config.");
            return false;
        }

        HostActionResult result = _session.ExportConfigAsync(_hostServices, GetType().Assembly.Location, target).GetAwaiter().GetResult();

        GH_RuntimeMessageLevel level = result.IsSuccess
            ? GH_RuntimeMessageLevel.Remark
            : GH_RuntimeMessageLevel.Warning;

        string message = result.RequiresManualAction
            ? result.ManualActionText ?? result.Message
            : result.Message;

        AddRuntimeMessage(level, message);
        return result.IsSuccess;
    }

    private HashSet<string> GetDesiredOutputNames()
    {
        return _tools.Select(static tool => tool.Name).ToHashSet(StringComparer.Ordinal);
    }
}

