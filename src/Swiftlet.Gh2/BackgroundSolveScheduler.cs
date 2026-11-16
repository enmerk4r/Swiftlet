using System.Runtime.CompilerServices;
using Eto.Forms;
using Grasshopper2.Components;
using Grasshopper2.Doc;

namespace Swiftlet.Gh2;

/// <summary>One pending entry per component; background delivery never cancels an active solution.</summary>
internal static class BackgroundSolveScheduler
{
    private static readonly ConditionalWeakTable<Document, Coordinator> Documents = new();

    /// <summary>Acquire an opaque coalescing lease; an old request can only release its own ticket.</summary>
    public static int Acquire(ref int pending, ref int sequence)
    {
        if (Volatile.Read(ref pending) != 0) return 0;
        int ticket = Interlocked.Increment(ref sequence);
        if (ticket == 0) ticket = Interlocked.Increment(ref sequence);
        return Interlocked.CompareExchange(ref pending, ticket, 0) == 0 ? ticket : 0;
    }

    public static void Request(Component component, Func<bool> current, Action? prepare = null, Action? release = null)
    {
        var request = new Pending(component, current, prepare, release);
        if (component.Document is not { } document) { request.Release(); return; }
        Documents.GetValue(document, static value => new Coordinator(value)).Enqueue(request);
    }

    private sealed class Pending
    {
        private Action? _release;
        public Component Component { get; }
        public Func<bool> Current { get; }
        public Action? Prepare { get; }
        public Pending(Component component, Func<bool> current, Action? prepare) : this(component, current, prepare, null) { }
        public Pending(Component component, Func<bool> current, Action? prepare, Action? release)
        { Component = component; Current = current; Prepare = prepare; _release = release; }
        public void Release() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }

    private sealed class Coordinator
    {
        private readonly Document _document;
        private readonly object _sync = new();
        private readonly Dictionary<Guid, Pending> _pending = new();
        private Timer? _timer;
        private bool _posted;
        private bool _initialized;
        private bool _disposed;
        private bool _waitingForStop;
        private volatile bool _stopObserved;

        public Coordinator(Document document) => _document = document;

        public void Enqueue(Pending request)
        {
            Pending? replaced = null;
            bool disposed, waiting;
            lock (_sync)
            {
                disposed = _disposed; waiting = _waitingForStop;
                if (!disposed)
                {
                    _pending.TryGetValue(request.Component.InstanceId, out replaced);
                    _pending[request.Component.InstanceId] = request;
                }
            }
            replaced?.Release();
            if (disposed) { request.Release(); return; }
            if (!waiting) Arm(15);
        }

        private void Arm(int milliseconds)
        {
            lock (_sync)
            {
                if (_disposed || _posted || _timer is not null) return;
                _timer = new Timer(_ => Post(), null, milliseconds, Timeout.Infinite);
            }
        }

        private void Post()
        {
            lock (_sync)
            {
                _timer?.Dispose(); _timer = null;
                if (_disposed || _posted) return;
                _posted = true;
            }
            // AsyncInvoke is deliberately non-blocking: a bridge reader must not wait for the UI
            // while teardown on the UI is waiting for that reader to finish.
            Application.Instance.AsyncInvoke(Pump);
        }

        private void Pump()
        {
            lock (_sync) { _posted = false; if (_disposed) return; }
            if (!_initialized)
            {
                _initialized = true;
                _document.StateChanged += OnDocumentStateChanged;
                _document.Solution.SolutionStarted += OnStarted;
                _document.Solution.SolutionStopped += OnStopped;
            }
            if (_document.State is DocumentState.Closing or DocumentState.Closed or DocumentState.Inert) { Dispose(); return; }
            if (_document.State != DocumentState.Active) return;
            if (_document.Solution.State.CurrentlyRunning is not null)
            {
                lock (_sync) _waitingForStop = true;
                // Stopped can be raised just before the server clears CurrentlyRunning. A single
                // short deferred check closes that ordering gap; new Started resets the signal.
                if (_stopObserved) Arm(10);
                return;
            }
            if (!SolutionServer.EnableSolutions) { Arm(250); return; }

            Pending[] batch;
            lock (_sync) { _waitingForStop = false; batch = _pending.Values.ToArray(); _pending.Clear(); }
            bool expired = false;
            foreach (var request in batch)
            {
                var component = request.Component;
                if (!ReferenceEquals(component.Document, _document) || component.Activity == ObjectActivity.Disabled || !request.Current()) { request.Release(); continue; }
                if (_document.Solution.State.CurrentlyRunning is not null) { Requeue(batch); return; }
                request.Release();
                request.Prepare?.Invoke();
                if (_document.Solution.State.CurrentlyRunning is not null) { Requeue(batch); return; }
                component.Expire();
                expired = true;
            }
            if (!expired) return;
            if (_document.Solution.State.CurrentlyRunning is not null) { Requeue(batch); return; }
            _stopObserved = false;
            _document.Solution.Start();
        }

        private void Requeue(IEnumerable<Pending> batch)
        {
            lock (_sync)
            {
                foreach (var request in batch)
                    _pending.TryAdd(request.Component.InstanceId, request);
                _waitingForStop = true;
            }
            // Stop may already have happened while the batch was outside _pending. One deferred
            // check is required even when OnStopped previously saw an empty dictionary.
            Arm(1);
        }

        private void OnStarted(object? sender, EventArgs args) => _stopObserved = false;
        private void OnStopped(object? sender, EventArgs args)
        {
            _stopObserved = true;
            lock (_sync) { _waitingForStop = false; if (_disposed || _pending.Count == 0) return; }
            Arm(1);
        }
        private void OnDocumentStateChanged(object? sender, DocumentStateEventArgs args)
        {
            if (args.NewState is DocumentState.Closing or DocumentState.Closed or DocumentState.Inert) Dispose();
            else if (args.NewState == DocumentState.Active) Arm(1);
        }
        private void Dispose()
        {
            Pending[] abandoned;
            lock (_sync)
            {
                if (_disposed) return;
                _disposed = true; abandoned = _pending.Values.ToArray(); _pending.Clear(); _timer?.Dispose(); _timer = null;
            }
            foreach (var request in abandoned) request.Release();
            if (_initialized)
            {
                _document.StateChanged -= OnDocumentStateChanged;
                _document.Solution.SolutionStarted -= OnStarted;
                _document.Solution.SolutionStopped -= OnStopped;
            }
            Documents.Remove(_document);
        }
    }
}
