using Eto.Forms;
using Grasshopper2.Components;
using Grasshopper2.Doc;
using Grasshopper2.Parameters;
using Grasshopper2.UI.InputPanel;
using Grasshopper2.Undo;
using GrasshopperIO;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Hosts.Rhino;
using Swiftlet.Runtime;
namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.McpServer)]
public sealed class McpServerComponent : SwiftletComponent, IRemovalAware
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly McpCallRequestState _calls = new();
    private ModernMcpServerSession? _session;
    private McpToolDefinition[] _desiredTools = [];
    private CancellationTokenSource _lifetime = new();
    private int _stopping;
    private int _updatePosted;
    private int _updateOutputs;
    private long _generation;

    public McpServerComponent() : base("MCP Server", "Starts an MCP server and exposes the connected tools to MCP clients. Each connected tool appears as its own output and emits a request when that tool is called.", Sections.Mcp) => Initialize();
    public McpServerComponent(IReader reader) : base(reader) => Initialize();
    private void Initialize()
    {
        Threading = ThreadingState.SingleThreaded;
        _calls.RequestReady += OnRequestQueued;
        DocumentChanged += OnDocumentChanged;
        ActivityChanged += (_, _) => { if (Activity == ObjectActivity.Disabled) StopServer(); else if (Document?.State == DocumentState.Active) Volatile.Write(ref _stopping, 0); };
    }
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddInteger("Port", "P", "Local TCP port for the MCP server. Use this port when connecting an MCP client to Swiftlet. Default: 3001.").Set(3001);
        inputs.Add(new McpToolDefinitionParameter("Tools", "T", "Tool definitions to expose to the MCP client. Each connected tool creates a matching request output on this component.", Access.Twig), Requirement.MayBeMissing);
        inputs.AddText("Server Name", "N", "Server name reported to the MCP client in the MCP connection info.", Access.Item, Requirement.MayBeMissing).Set("Swiftlet");
    }
    protected override void AddOutputs(OutputAdder outputs) { }
    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out int port); access.GetItem(2, out string name);
        access.GetTwig<McpToolDefinition>(1, out var toolData);
        var tools = toolData?.NonNullItems.Select(t => t.Duplicate()).ToArray() ?? [];
        access.Solution.Token.ThrowIfCancellationRequested();
        _gate.Wait(access.Solution.Token);
        try
        {
            if (Volatile.Read(ref _stopping) != 0 || Document is null || Activity == ObjectActivity.Disabled) return;
            if (port < 1 || port > 65535) { StopUnderGate(); access.AddError("Invalid port", "Port number must be between 1 and 65535"); return; }
            Volatile.Write(ref _desiredTools, tools);
            if (NeedsOutputUpdate(tools)) { ScheduleUpdate(true); return; }
            if (_lifetime.IsCancellationRequested) { _lifetime.Dispose(); _lifetime = new(); }
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, access.Solution.Token);
            _session ??= CreateSession();
            if (_session.Port != port) _calls.Clear();
            _session.ReconfigureAsync(port, name, tools, linked.Token).GetAwaiter().GetResult();
            ShowMessage(_session.StatusMessage);
            var names = Parameters.Outputs.Select(p => p.UserName).ToArray();
            var calls = _calls.Capture(names, key => _session.TryDequeuePendingCall(key, out var call) ? call : null);
            linked.Token.ThrowIfCancellationRequested();
            for (int index = 0; index < calls.Length; index++) access.SetItem(index, calls[index]!);
        }
        catch (OperationCanceledException) when (access.Solution.Token.IsCancellationRequested || _lifetime.IsCancellationRequested) { throw; }
        catch (Exception error) { access.AddError("MCP server failed", $"Failed to start MCP server: {error.Message}"); ShowMessage("Error"); }
        finally { _gate.Release(); }
    }
    private ModernMcpServerSession CreateSession()
    { var session = new ModernMcpServerSession(); session.RequestQueued += OnRequestQueued; return session; }
    private bool NeedsOutputUpdate(IEnumerable<McpToolDefinition> tools)
        => !Parameters.Outputs.Select(p => p.UserName).ToHashSet(StringComparer.Ordinal).SetEquals(tools.Select(t => t.Name));
    private void SynchronizeOutputs()
    {
        var tools = Volatile.Read(ref _desiredTools);
        var names = tools.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        for (int index = Parameters.OutputCount - 1; index >= 0; index--)
            if (!names.Contains(Parameters.Output(index).UserName)) Parameters.RemoveOutput(index, null!);
        var current = Parameters.Outputs.Select(p => p.UserName).ToHashSet(StringComparer.Ordinal);
        foreach (var tool in tools)
            if (current.Add(tool.Name)) Parameters.AddOutput(new McpToolCallRequestParameter(tool.Name, tool.Name,
                $"Emits a request when the MCP client calls '{tool.Name}'. Connect it to Deconstruct Tool Call, process the request, then finish with MCP Tool Response."), null!);
        InvalidateExistingLayout();
    }
    private void OnRequestQueued(object? sender, EventArgs args)
    { if (sender is ModernMcpServerSession session && !ReferenceEquals(session, _session)) return; ScheduleUpdate(false); }
    private void ScheduleUpdate(bool outputs)
    {
        if (outputs) Interlocked.Exchange(ref _updateOutputs, 1);
        long generation = Volatile.Read(ref _generation);
        if (Volatile.Read(ref _stopping) != 0 || Interlocked.Exchange(ref _updatePosted, 1) != 0) return;
        BackgroundSolveScheduler.Request(this,
            () => generation == Volatile.Read(ref _generation) && Volatile.Read(ref _stopping) == 0,
            () =>
            {
                if (Interlocked.Exchange(ref _updateOutputs, 0) != 0) SynchronizeOutputs();
            },
            release: () => { if (generation == Volatile.Read(ref _generation)) Interlocked.Exchange(ref _updatePosted, 0); });
    }
    public override bool CanCreateParameter(Side side, int index) => false;
    public override bool CanRemoveParameter(Side side, int index) => false;
    public void PrepareForRemoval(ActionList undo) => StopServer();
    private void OnDocumentChanged(object? sender, ObjectDocumentEventArgs args)
    {
        if (args.OldDocument is { } old) { old.StateChanged -= OnDocumentStateChanged; StopServer(); }
        if (args.NewDocument is { } current) { current.StateChanged += OnDocumentStateChanged; Volatile.Write(ref _stopping, 0); }
    }
    private void OnDocumentStateChanged(object? sender, DocumentStateEventArgs args)
    {
        if (args.NewState is DocumentState.Closing or DocumentState.Closed or DocumentState.Inert) StopServer();
        else if (args.NewState == DocumentState.Active) Volatile.Write(ref _stopping, 0);
    }
    private void StopServer()
    {
        Volatile.Write(ref _stopping, 1); Interlocked.Increment(ref _generation); Interlocked.Exchange(ref _updatePosted, 0);
        try { Volatile.Read(ref _lifetime).Cancel(); } catch (ObjectDisposedException) { }
        _gate.Wait(); try { StopUnderGate(); } finally { _gate.Release(); }
    }
    private void StopUnderGate()
    {
        _calls.Clear(); if (_session is not { } session) return;
        _session = null; session.RequestQueued -= OnRequestQueued; session.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
    public string GenerateClientConfig(McpClientConfigTarget target)
        => (_session ?? new ModernMcpServerSession()).GenerateConfig(GetType().Assembly.Location, target);
    public override void AppendToInputPanel(InputPanel panel)
    {
        base.AppendToInputPanel(panel); panel.AddLabel("MCP Config", false, "Copy a client configuration for this server.");
        foreach (var target in Enum.GetValues<McpClientConfigTarget>())
        {
            var copy = new Button { Text = $"Copy {target} config" };
            copy.Click += async (_, _) =>
            {
                try { await ModernMcpWorkflow.ExportConfigAsync(new RhinoHostServices(), GenerateClientConfig(target)); }
                catch (Exception error) { Rhino.RhinoApp.WriteLine($"Failed to generate MCP config: {error.Message}"); }
            };
            panel.Add(copy);
        }
    }
}

