using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class ListenerRequestGoo : GH_Goo<ModernServerRequestContext>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "Listener Request";

    public override string TypeDescription => "Contains HTTP request and response state for Swiftlet's server components";

    public ListenerRequestGoo()
    {
        Value = default!;
    }

    public ListenerRequestGoo(ModernServerRequestContext? context)
    {
        Value = context ?? default!;
    }

    public override IGH_Goo Duplicate() => new ListenerRequestGoo(Value);

    public override string ToString() => "LISTENER REQUEST";
}
