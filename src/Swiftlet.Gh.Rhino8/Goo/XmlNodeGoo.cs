using System.Text;
using System.Xml;
using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class XmlNodeGoo : GH_Goo<XmlNode>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "XML Node";

    public override string TypeDescription => "A queryable XML node";

    public XmlNodeGoo()
    {
        Value = default!;
    }

    public XmlNodeGoo(XmlNode? node)
    {
        Value = node;
    }

    public override IGH_Goo Duplicate() => new XmlNodeGoo(Value);

    public override string ToString() => Value is null ? "Null XML Node" : $"XML Node [ {Value.Name} ]";

    public string ToXmlString(bool indent = true)
    {
        if (Value is null)
        {
            return string.Empty;
        }

        if (!indent)
        {
            return Value.OuterXml;
        }

        var builder = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = true,
        };

        using XmlWriter writer = XmlWriter.Create(builder, settings);
        Value.WriteTo(writer);
        writer.Flush();
        return builder.ToString();
    }

    public override bool CastTo<Q>(ref Q target)
    {
        if (typeof(Q) == typeof(GH_String) && Value is not null)
        {
            object temp = new GH_String(Value.OuterXml);
            target = (Q)temp;
            return true;
        }

        return base.CastTo(ref target);
    }
}
