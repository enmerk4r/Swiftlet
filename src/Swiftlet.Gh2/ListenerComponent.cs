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
    private int _abandonedRetry = 1;

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
            // The previous publication may still be a valid cached result, even after the enclosing
            // solution was cancelled. Confirm it before a new Publish can replace its identity.
            ConfirmCurrentPublication();
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
        Volatile.Write(ref _abandonedRetry, 1);
        QueueRecompute();
    }

    private void QueueRecompute()
    {
        long generation = Volatile.Read(ref _generation);
        if (Volatile.Read(ref _stopping) != 0 || Interlocked.Exchange(ref _expiryPosted, 1) != 0) return;
        BackgroundSolveScheduler.Request(this,
            () => generation == Volatile.Read(ref _generation) && Volatile.Read(ref _stopping) == 0,
            release: () => { if (generation == Volatile.Read(ref _generation)) Interlocked.Exchange(ref _expiryPosted, 0); });
    }

    public void PrepareForRemoval(ActionList undo) => StopListener();

    /// <summary>Confirm a publication whose own component result is the current successful cache.</summary>
    protected virtual void OnSolutionCommitted(SolutionId solution) { }

    /// <summary>True while network items have no retained successful component result.</summary>
    protected virtual bool HasUnconfirmedOutput => false;

    private void ConfirmCurrentPublication()
    {
        // Data is the most recent successful component result. CurrentId identifies that result;
        // ProcessId and Phase can instead describe a newer cancelled/faulted computation.
        var state = State;
        if (state.Data is not null && state.CurrentId != SolutionId.None)
            OnSolutionCommitted(state.CurrentId);
    }

    private void OnSolutionEnded(object? sender, EventArgs args)
    {
        ConfirmCurrentPublication();
        // At most one automatic retry until another real notification arrives. A persistent
        // abandoned solve must not produce a self-sustaining retry loop.
        if (HasUnconfirmedOutput && Interlocked.CompareExchange(ref _abandonedRetry, 0, 1) == 1)
            QueueRecompute();
    }

    /// <summary>
    /// Items received from the network that remain pending until their publishing component result
    /// becomes the current successful cache. Whole-solution cancellation does not invalidate a
    /// component result that already completed. Discarded publications are retried; retained ones
    /// are confirmed, so a later solution can consume their cache without republishing the items.
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

        /// <summary>True while published or newly stored items are waiting for a retained successful component result.</summary>
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
            old.Solution.SolutionCompleted -= OnSolutionEnded;
            old.Solution.SolutionCancelled -= OnSolutionEnded;
            old.Solution.SolutionFaulted -= OnSolutionEnded;
            StopListener();
        }

        if (args.NewDocument is { } current)
        {
            current.StateChanged += OnDocumentStateChanged;
            current.Solution.SolutionCompleted += OnSolutionEnded;
            current.Solution.SolutionCancelled += OnSolutionEnded;
            current.Solution.SolutionFaulted += OnSolutionEnded;
            Volatile.Write(ref _stopping, 0);
            Volatile.Write(ref _abandonedRetry, 1);
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
        Volatile.Write(ref _abandonedRetry, 0);
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
