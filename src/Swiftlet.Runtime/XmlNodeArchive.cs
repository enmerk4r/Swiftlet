using System.Xml;

namespace Swiftlet.Runtime;

/// <summary>
/// The text a saved XML node is stored as, and how it is read back. An element is stored as its own
/// markup, as it always was, and comes back as the root element of a new document. Any other kind of
/// node (text, attribute, comment, CDATA, processing instruction, document, ...) cannot be parsed on its
/// own, so it is stored inside an envelope element in Swiftlet's namespace that records its kind,
/// name and value, and comes back as a new node of that kind owned by a new document.
/// </summary>
internal static class XmlNodeArchive
{
    public const string EnvelopeNamespace = "urn:swiftlet:xml-node";
    private const string EnvelopeName = "node";

    public static string Encode(XmlNode node)
    {
        if (node is XmlElement)
        {
            return node.OuterXml;
        }

        var document = new XmlDocument();
        XmlElement envelope = document.CreateElement(EnvelopeName, EnvelopeNamespace);
        void Set(string name, string? value)
        {
            if (value is not null)
            {
                envelope.SetAttribute(name, value);
            }
        }

        switch (node)
        {
            case XmlAttribute attribute:
                Set("kind", "Attribute");
                Set("name", attribute.Name);
                Set("ns", attribute.NamespaceURI);
                Set("value", attribute.Value);
                break;
            case XmlProcessingInstruction instruction:
                Set("kind", "ProcessingInstruction");
                Set("name", instruction.Target);
                Set("value", instruction.Data);
                break;
            case XmlDeclaration declaration:
                Set("kind", "XmlDeclaration");
                Set("version", declaration.Version);
                Set("encoding", declaration.Encoding);
                Set("standalone", declaration.Standalone);
                break;
            case XmlEntityReference reference:
                Set("kind", "EntityReference");
                Set("name", reference.Name);
                break;
            case XmlDocument or XmlDocumentFragment:
                Set("kind", node.NodeType.ToString());
                Set("value", node.InnerXml);
                break;
            case XmlText or XmlCDataSection or XmlComment or XmlWhitespace or XmlSignificantWhitespace:
                Set("kind", node.NodeType.ToString());
                Set("value", node.Value);
                break;
            default:
                // Document types, entities and notations: keep their markup as text.
                Set("kind", "Text");
                Set("value", node.OuterXml);
                break;
        }

        return envelope.OuterXml;
    }

    /// <summary>Rebuilds a node from <see cref="Encode"/> output, or from what older versions stored. Null if it cannot.</summary>
    public static XmlNode? Decode(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var document = new XmlDocument();
        try
        {
            document.LoadXml(text);
        }
        catch (XmlException)
        {
            return DecodeLegacy(text);
        }

        XmlElement? root = document.DocumentElement;
        if (root is null)
        {
            return null;
        }

        if (root.LocalName != EnvelopeName || root.NamespaceURI != EnvelopeNamespace)
        {
            return root;
        }

        string? Get(string name) => root.HasAttribute(name) ? root.GetAttribute(name) : null;
        string value = Get("value") ?? string.Empty;
        var owner = new XmlDocument { PreserveWhitespace = true };
        try
        {
            switch (Get("kind"))
            {
                case "Attribute":
                    XmlAttribute attribute = owner.CreateAttribute(Get("name") ?? string.Empty, Get("ns") ?? string.Empty);
                    attribute.Value = value;
                    return attribute;
                case "ProcessingInstruction":
                    return owner.CreateProcessingInstruction(Get("name") ?? string.Empty, value);
                case "XmlDeclaration":
                    return owner.CreateXmlDeclaration(Get("version") ?? "1.0", Get("encoding"), Get("standalone"));
                case "EntityReference":
                    return owner.CreateEntityReference(Get("name") ?? string.Empty);
                case "Document":
                    if (value.Length > 0)
                    {
                        owner.LoadXml(value);
                    }

                    return owner;
                case "DocumentFragment":
                    XmlDocumentFragment fragment = owner.CreateDocumentFragment();
                    fragment.InnerXml = value;
                    return fragment;
                case "CDATA":
                    return owner.CreateCDataSection(value);
                case "Comment":
                    return owner.CreateComment(value);
                case "Whitespace":
                    return owner.CreateWhitespace(value);
                case "SignificantWhitespace":
                    return owner.CreateSignificantWhitespace(value);
                case "Text":
                    return owner.CreateTextNode(value);
                default:
                    return null;
            }
        }
        catch (Exception ex) when (ex is XmlException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    // Before the envelope, every node was stored as its bare markup, which only an element survives.
    // Recover what can be recovered: an attribute without a prefix (or a namespace declaration) parses
    // on an element (tried first, since name="value" also parses as text), and text, CDATA, comments and
    // processing instructions parse as fragment content.
    private static XmlNode? DecodeLegacy(string text)
    {
        var owner = new XmlDocument { PreserveWhitespace = true };
        try
        {
            var holder = new XmlDocument();
            holder.LoadXml("<x " + text + "/>");
            if (holder.DocumentElement!.Attributes.Count == 1)
            {
                XmlAttribute saved = holder.DocumentElement.Attributes[0];
                XmlAttribute attribute = owner.CreateAttribute(saved.Name, saved.NamespaceURI);
                attribute.Value = saved.Value;
                return attribute;
            }
        }
        catch (XmlException)
        {
        }

        try
        {
            XmlDocumentFragment fragment = owner.CreateDocumentFragment();
            fragment.InnerXml = text;
            if (fragment.ChildNodes.Count == 1 && fragment.FirstChild is { } only)
            {
                return fragment.RemoveChild(only);
            }
        }
        catch (XmlException)
        {
        }

        return null;
    }
}
