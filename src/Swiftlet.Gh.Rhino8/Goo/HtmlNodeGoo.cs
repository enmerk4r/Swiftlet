using Grasshopper.Kernel.Types;
using HtmlAgilityPack;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class HtmlNodeGoo : GH_Goo<HtmlNode>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "HTML Node";

    public override string TypeDescription => "A queryable HTML node";

    public HtmlNodeGoo()
    {
        Value = default!;
    }

    public HtmlNodeGoo(HtmlNode? node)
    {
        Value = node;
    }

    public override IGH_Goo Duplicate() => new HtmlNodeGoo(Value);

    public override string ToString() => $"HTML Node [ {Value?.Name} ]";
}
