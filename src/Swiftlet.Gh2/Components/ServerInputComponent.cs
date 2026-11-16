using Grasshopper2.Components;
using Grasshopper2.Doc;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using Swiftlet.Gh2.Icons;
using Grasshopper2.Undo;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.ServerInput)]
public sealed class ServerInputComponent : Component, IRemovalAware
{
    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private readonly ServerInputRequestState _requests = new();
    private ModernServerSession? _session;
    private CancellationTokenSource _lifetime = new();
    private int _stopping;
    private int _expiryPosted;
    private long _generation;

    public ServerInputComponent()
        : base(Palette.Entry(nameof(ServerInputComponent), "Server Input", "Listen for HTTP requests and route them to variable outputs.", "Swiftlet", "8. Server")) => Initialize();
    public ServerInputComponent(IReader reader) : base(reader) => Initialize();

    private void Initialize()
    {
        Threading = ThreadingState.SingleThreaded;
        DocumentChanged += OnDocumentChanged;
        ActivityChanged += (_, _) =>
        {
            if (Activity == ObjectActivity.Disabled) StopListener();
            else if (Document is { State: DocumentState.Active }) Volatile.Write(ref _stopping, 0);
        };
        _requests.RequestReady += OnRequestQueued;
    }

    protected override void AddInputs(InputAdder inputs)
        => inputs.AddInteger("Port", "P", "TCP port to listen on (1-65535).").Set(8080);
    protected override void AddOutputs(OutputAdder outputs)
        => outputs.Add(new ListenerRequestParameter("Root", "/", "Default route for requests to /."));

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out int port)) return;
        access.Solution.Token.ThrowIfCancellationRequested();
        _sessionGate.Wait(access.Solution.Token);
        try
        {
            if (Volatile.Read(ref _stopping) != 0 || Document is null || Activity == ObjectActivity.Disabled) return;
            if (port < 1 || port > 65535)
            {
                StopSessionUnderGate();
                access.AddError("Invalid port", "Port number must be between 1 and 65535.");
                return;
            }
            if (_lifetime.IsCancellationRequested) { _lifetime.Dispose(); _lifetime = new(); }
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Solution.Token, _lifetime.Token);
            string[] routes = Parameters.Outputs.Select(p => ModernServerRouteMatcher.NormalizeRoute(p.UserName)).ToArray();
            _session ??= CreateSession();
            if (_session.Port != port) _requests.Clear();
            _session.ReconfigureAsync(port, routes, linked.Token).GetAwaiter().GetResult();
            var requests = _requests.Capture(routes, route => _session.TryDequeuePendingRequest(route, out var request) ? request : null);
            linked.Token.ThrowIfCancellationRequested();
            for (int index = 0; index < requests.Length; index++)
                if (requests[index] is { } request) access.SetItem(index, request);
        }
        catch (OperationCanceledException) when (access.Solution.Token.IsCancellationRequested || _lifetime.IsCancellationRequested) { throw; }
        catch (Exception error) { access.AddError("Server failed", error.Message); }
        finally { _sessionGate.Release(); }
    }

    private ModernServerSession CreateSession()
    {
        var session = new ModernServerSession();
        session.RequestQueued += OnRequestQueued;
        return session;
    }

    public override bool CanCreateParameter(Side side, int index)
        => side == Side.Output && index > 0 && index <= Parameters.OutputCount;
    public override bool CanRemoveParameter(Side side, int index)
        => side == Side.Output && index > 0 && index < Parameters.OutputCount;
    public override void DoCreateParameter(Side side, int index, ActionList undo)
    {
        if (!CanCreateParameter(side, index)) return;
        Parameters.AddOutput(new ListenerRequestParameter($"Route {index}", "/new-route", "HTTP route endpoint."), index, undo);
    }
    public override void VariableParameterMaintenance()
    {
        foreach (var parameter in Parameters.Outputs)
            parameter.UserName = ModernServerRouteMatcher.NormalizeRoute(parameter.UserName);
    }
    public void PrepareForRemoval(ActionList undo) => StopListener();

    private void OnDocumentChanged(object? sender, ObjectDocumentEventArgs args)
    {
        if (args.OldDocument is { } old)
        {
            old.StateChanged -= OnDocumentStateChanged;
            StopListener();
        }
        if (args.NewDocument is { } current)
        {
            current.StateChanged += OnDocumentStateChanged;
            Volatile.Write(ref _stopping, 0);
        }
    }
    private void OnDocumentStateChanged(object? sender, DocumentStateEventArgs args)
    {
        if (args.NewState is DocumentState.Closing or DocumentState.Closed or DocumentState.Inert) StopListener();
        else if (args.NewState == DocumentState.Active) Volatile.Write(ref _stopping, 0);
    }
    private void OnRequestQueued(object? sender, EventArgs args)
    {
        if (sender is ModernServerSession source && !ReferenceEquals(source, _session)) return;
        long generation = Volatile.Read(ref _generation);
        if (Volatile.Read(ref _stopping) != 0 || Interlocked.Exchange(ref _expiryPosted, 1) != 0) return;
        Rhino.RhinoApp.InvokeOnUiThread((System.Action)(() =>
        {
            if (generation != Volatile.Read(ref _generation)) return;
            Interlocked.Exchange(ref _expiryPosted, 0);
            var document = Document;
            if (Volatile.Read(ref _stopping) != 0 || document is null ||
                document.State != DocumentState.Active || Activity == ObjectActivity.Disabled) return;
            Expire();
            document.Solution.Start();
        }));
    }
    private void StopListener()
    {
        Volatile.Write(ref _stopping, 1);
        Interlocked.Increment(ref _generation);
        Interlocked.Exchange(ref _expiryPosted, 0);
        try { Volatile.Read(ref _lifetime).Cancel(); } catch (ObjectDisposedException) { }
        _sessionGate.Wait();
        try { StopSessionUnderGate(); }
        finally { _sessionGate.Release(); }
    }
    private void StopSessionUnderGate()
    {
        _requests.Clear();
        if (_session is not { } session) return;
        _session = null;
        session.RequestQueued -= OnRequestQueued;
        session.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
