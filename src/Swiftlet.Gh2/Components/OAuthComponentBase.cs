using Grasshopper2.Components;
using Grasshopper2.Doc;
using Grasshopper2.UI;
using Grasshopper2.Undo;
using GrasshopperIO;

namespace Swiftlet.Gh2.Components;

public abstract class OAuthComponentBase : Component, IRemovalAware
{
    private CancellationTokenSource _lifetime = new();
    protected CancellationToken LifetimeToken => Volatile.Read(ref _lifetime).Token;
    protected OAuthComponentBase(Nomen nomen) : base(nomen) => Initialize();
    protected OAuthComponentBase(IReader reader) : base(reader) => Initialize();
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    private void Initialize()
    {
        Threading = ThreadingState.SingleThreaded;
        DocumentChanged += (_, args) =>
        {
            if (args.OldDocument is { } old) { old.StateChanged -= StateChanged; Stop(); }
            if (args.NewDocument is { } current) { current.StateChanged += StateChanged; Resume(); }
        };
        ActivityChanged += (_, _) => { if (Activity == ObjectActivity.Disabled) Stop(); else Resume(); };
    }
    private void StateChanged(object? sender, DocumentStateEventArgs args)
    {
        if (args.NewState is DocumentState.Closing or DocumentState.Closed or DocumentState.Inert) Stop();
        else if (args.NewState == DocumentState.Active) Resume();
    }
    private void Resume()
    {
        if (_lifetime.IsCancellationRequested) Interlocked.Exchange(ref _lifetime, new CancellationTokenSource());
    }
    private void Stop() { Volatile.Read(ref _lifetime).Cancel(); OnStopped(); }
    protected virtual void OnStopped() { }
    public void PrepareForRemoval(ActionList undo) => Stop();
}
