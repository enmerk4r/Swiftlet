using Grasshopper2.Components;
using Grasshopper2.Doc;
using Grasshopper2.Undo;
using GrasshopperIO;

namespace Swiftlet.Gh2;

/// <summary>
/// Base for components that own a live network resource (a listener, socket or server) and recompute
/// when something arrives on a background thread. It implements the lifecycle proven by Server Input:
///
/// - Iterations run one at a time (<see cref="ThreadingState.SingleThreaded"/>), and Process, shutdown
///   and reconfiguration are serialised by a gate.
/// - The resource is released when the component is deleted, disabled, moved out of its document, or
///   when the document closes; it may be created again later (undo, re-enable, reopen).
/// - Background notifications are coalesced into one recompute, marshalled to the UI thread, and
///   dropped if they belong to a resource that has since been stopped.
/// </summary>
public abstract class ListenerComponent : SwiftletComponent, IRemovalAware
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource _lifetime = new();
    private int _stopping;
    private int _expiryPosted;
    private long _generation;

    protected ListenerComponent(string name, string info, string section)
        : base(name, info, section) => Initialize();

    protected ListenerComponent(IReader reader)
        : base(reader) => Initialize();

    private void Initialize()
    {
        Threading = ThreadingState.SingleThreaded;
        DocumentChanged += OnDocumentChanged;
        ActivityChanged += (_, _) =>
        {
            if (Activity == ObjectActivity.Disabled)
                StopListener();
            else if (Document is { State: DocumentState.Active })
                Volatile.Write(ref _stopping, 0);
        };
    }

    /// <summary>
    /// Do the work of one solve. Called with the gate held and only while the component may run.
    /// <paramref name="cancellation"/> fires when the solution is cancelled or the component is being stopped;
    /// pass it to every network call so shutdown never waits behind a blocked solve.
    /// </summary>
    protected abstract void ProcessListener(IDataAccess access, CancellationToken cancellation);

    /// <summary>Release the network resource. Called with the gate held; must tolerate repeated calls.</summary>
    protected abstract void StopResource();

    protected sealed override void Process(IDataAccess access)
    {
        access.Solution.Token.ThrowIfCancellationRequested();
        _gate.Wait(access.Solution.Token);
        try
        {
            if (Volatile.Read(ref _stopping) != 0 || Document is null || Activity == ObjectActivity.Disabled)
                return;

            if (_lifetime.IsCancellationRequested)
            {
                _lifetime.Dispose();
                _lifetime = new CancellationTokenSource();
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Solution.Token, _lifetime.Token);
            ProcessListener(access, linked.Token);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Ask for a recompute from any thread. Several calls before the recompute starts collapse into one.
    /// </summary>
    protected void RequestRecompute()
    {
        long generation = Volatile.Read(ref _generation);
        if (Volatile.Read(ref _stopping) != 0 || Interlocked.Exchange(ref _expiryPosted, 1) != 0)
            return;

        Rhino.RhinoApp.InvokeOnUiThread((System.Action)(() =>
        {
            if (generation != Volatile.Read(ref _generation))
                return;

            Interlocked.Exchange(ref _expiryPosted, 0);
            Document? document = Document;
            if (Volatile.Read(ref _stopping) != 0 || document is null ||
                document.State != DocumentState.Active || Activity == ObjectActivity.Disabled)
                return;

            Expire();
            document.Solution.Start();
        }));
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
        if (args.NewState is DocumentState.Closing or DocumentState.Closed or DocumentState.Inert)
            StopListener();
        else if (args.NewState == DocumentState.Active)
            Volatile.Write(ref _stopping, 0);
    }

    private void StopListener()
    {
        Volatile.Write(ref _stopping, 1);
        Interlocked.Increment(ref _generation);
        Interlocked.Exchange(ref _expiryPosted, 0);
        try
        {
            Volatile.Read(ref _lifetime).Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        _gate.Wait();
        try
        {
            StopResource();
        }
        finally
        {
            _gate.Release();
        }
    }
}
