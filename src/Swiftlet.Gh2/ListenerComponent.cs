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

    /// <summary>
    /// Called when a solution has completed successfully, i.e. the outputs this component set in it
    /// have really been delivered. Use it to forget data that was only waiting for that confirmation.
    /// </summary>
    protected virtual void OnSolutionCommitted(SolutionId solution) { }

    /// <summary>
    /// True while something this component published has not been confirmed by a completed solution.
    /// When a solution is cancelled or faults in that state, the component asks to be solved again,
    /// because GH2 does not necessarily run it again by itself.
    /// </summary>
    protected virtual bool HasUnconfirmedOutput => false;

    private void OnSolutionCompleted(object? sender, SolutionEventArgs args) => OnSolutionCommitted(args.SolutionId);

    private void OnSolutionAbandoned(object? sender, EventArgs args)
    {
        if (HasUnconfirmedOutput)
            RequestRecompute();
    }

    /// <summary>
    /// Items received from the network that must reach the outputs exactly once.
    ///
    /// A GH2 solution can be cancelled after Process has set its outputs (for example when another
    /// component expires). Outputs of a cancelled solution are discarded, so anything taken off a
    /// queue during it would be lost. An outbox keeps what a solve published until that solve is
    /// confirmed complete; a solve that follows a cancelled one publishes the same items again,
    /// followed by anything newer.
    /// </summary>
    protected sealed class Outbox<T>
    {
        private readonly object _sync = new();
        private readonly List<T> _items = [];
        private SolutionId _publishedIn = SolutionId.None;
        private int _publishedCount;

        /// <summary>
        /// Moves newly arrived items into the outbox and returns everything not yet confirmed, oldest first.
        /// </summary>
        public T[] Publish(Solution solution, TryDequeue dequeue)
        {
            lock (_sync)
            {
                while (dequeue(out T item))
                    _items.Add(item);

                _publishedIn = solution.Id;
                _publishedCount = _items.Count;
                return _items.ToArray();
            }
        }

        /// <summary>Forgets the items published in <paramref name="solution"/>, if that was the latest publication.</summary>
        public void Commit(SolutionId solution)
        {
            lock (_sync)
            {
                if (_publishedCount == 0 || _publishedIn != solution)
                    return;

                _items.RemoveRange(0, _publishedCount);
                _publishedCount = 0;
                _publishedIn = SolutionId.None;
            }
        }

        /// <summary>True while published or newly stored items are waiting for a completed solution.</summary>
        public bool HasUnconfirmed
        {
            get
            {
                lock (_sync)
                {
                    return _items.Count > 0;
                }
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _items.Clear();
                _publishedCount = 0;
                _publishedIn = SolutionId.None;
            }
        }

        public delegate bool TryDequeue(out T item);
    }

    private void OnDocumentChanged(object? sender, ObjectDocumentEventArgs args)
    {
        if (args.OldDocument is { } old)
        {
            old.StateChanged -= OnDocumentStateChanged;
            old.Solution.SolutionCompleted -= OnSolutionCompleted;
            old.Solution.SolutionCancelled -= OnSolutionAbandoned;
            old.Solution.SolutionFaulted -= OnSolutionAbandoned;
            StopListener();
        }

        if (args.NewDocument is { } current)
        {
            current.StateChanged += OnDocumentStateChanged;
            current.Solution.SolutionCompleted += OnSolutionCompleted;
            current.Solution.SolutionCancelled += OnSolutionAbandoned;
            current.Solution.SolutionFaulted += OnSolutionAbandoned;
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
