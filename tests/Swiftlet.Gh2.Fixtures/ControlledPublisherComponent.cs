using System.Collections.Concurrent;
using Grasshopper2.Components;
using Grasshopper2.Doc;
using Grasshopper2.Parameters;
using GrasshopperIO;
using Swiftlet.Gh2;

namespace Swiftlet.Gh2.Fixtures;

// Process-local validation assembly only; never referenced by the product or Yak payload.
[IoId("a3edb0dc-f32f-4b7c-b46d-66d9c945cb6f")]
public sealed class ControlledPublisherComponent : ListenerComponent
{
    private readonly ConcurrentQueue<string> _arrived = new();
    private readonly Outbox<string> _pending = new();
    public bool CancelAfterPublish { get; set; }
    public int Attempts { get; private set; }
    public int CancelledPublications { get; private set; }
    public bool Pending => _pending.HasUnconfirmed;
    public ControlledPublisherComponent() : base("Controlled publisher", "Cancellation regression fixture", "Tests") { }
    public ControlledPublisherComponent(IReader reader) : base(reader) { }
    public void Enqueue(string value) => _arrived.Enqueue(value);
    protected override void AddInputs(InputAdder inputs) { }
    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Messages", "M", "Fixture messages", Access.Twig);
    protected override void ProcessListener(IDataAccess access, CancellationToken cancellation)
    {
        Attempts++;
        var items = _pending.Publish(access.Solution, _arrived.TryDequeue!);
        if (CancelAfterPublish)
        {
            CancelAfterPublish = false;
            cancellation.WaitHandle.WaitOne();
            CancelledPublications++;
            cancellation.ThrowIfCancellationRequested();
        }
        access.SetTwig(0, items);
    }
    protected override void OnSolutionCommitted(SolutionId solution) => _pending.Commit(solution);
    protected override bool HasUnconfirmedOutput => _pending.HasUnconfirmed;
    protected override void StopResource() { _pending.Clear(); _arrived.Clear(); }
}
