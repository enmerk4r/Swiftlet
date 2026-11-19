using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using GrasshopperIO;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Types;

public sealed class ListenerRequestTypeAssistant : TypeAssistant<ModernServerRequestContext>
{
    public ListenerRequestTypeAssistant() : base("Listener Request") { }
    // Copies share the same one-shot response handle, as GH1 ListenerRequestGoo.Duplicate does.
    public override ModernServerRequestContext Copy(ModernServerRequestContext instance) => instance;
    public override string DescribePrimary(Pear<ModernServerRequestContext> pear) => Format(pear.Item);
    public override string Format(ModernServerRequestContext instance) => "LISTENER REQUEST";
    // Live network connections cannot survive saving/reopening a definition.
    public override bool Write(IWriter writer, Name location, ModernServerRequestContext instance) => false;
    public override bool Read(IReader reader, Name location, out ModernServerRequestContext instance)
    { instance = null!; return false; }
}
