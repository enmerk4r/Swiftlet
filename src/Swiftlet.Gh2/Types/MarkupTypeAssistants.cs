using System.Xml;
using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using GrasshopperIO;
using HtmlAgilityPack;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Types;

// HTML and XML nodes keep their place in the parsed document (parents, siblings, XPath context),
// exactly as in Grasshopper 1, so Copy hands back the same node. Components must treat nodes as
// read-only. When saved, an HTML node or XML element is stored as its markup and comes back as the root
// of a new document; other XML nodes (text, attributes, comments, ...) are stored with their kind, see
// XmlNodeArchive, and come back as a node of that kind owned by a new document.

public sealed class HtmlNodeTypeAssistant : TypeAssistant<HtmlNode>
{
    public HtmlNodeTypeAssistant() : base("HTML Node") { }

    public override HtmlNode Copy(HtmlNode instance) => instance;

    public override string DescribePrimary(Pear<HtmlNode> pear) => Format(pear.Item);

    public override string Format(HtmlNode instance) => $"HTML Node [ {instance.Name} ]";

    public override bool Write(IWriter writer, Name location, HtmlNode instance)
    {
        writer.String(location, instance.OuterHtml);
        return true;
    }

    public override bool Read(IReader reader, Name location, out HtmlNode instance)
    {
        instance = null!;
        if (!reader.HasItem(location))
            return false;

        var document = new HtmlDocument();
        document.LoadHtml(reader.String(location));
        HtmlNode root = document.DocumentNode;

        // A saved element comes back as the single child of the new document node.
        instance = root.ChildNodes.Count == 1 ? root.ChildNodes[0] : root;
        return true;
    }
}

public sealed class XmlNodeTypeAssistant : TypeAssistant<XmlNode>
{
    public XmlNodeTypeAssistant() : base("XML Node") { }

    public override XmlNode Copy(XmlNode instance) => instance;

    public override string DescribePrimary(Pear<XmlNode> pear) => Format(pear.Item);

    public override string Format(XmlNode instance) => $"XML Node [ {instance.Name} ]";

    public override bool Write(IWriter writer, Name location, XmlNode instance)
    {
        writer.String(location, XmlNodeArchive.Encode(instance));
        return true;
    }

    public override bool Read(IReader reader, Name location, out XmlNode instance)
    {
        instance = null!;
        if (!reader.HasItem(location))
            return false;

        XmlNode? node = XmlNodeArchive.Decode(reader.String(location));
        if (node is null)
            return false;

        instance = node;
        return true;
    }
}
