using System.Text;
using System.Xml;
using HtmlAgilityPack;

namespace Swiftlet.Runtime;

/// <summary>HTML node queries shared by the Grasshopper 1 and Grasshopper 2 shells.</summary>
internal static class HtmlQueries
{
    public static HtmlNode Parse(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html ?? string.Empty);
        return document.DocumentNode;
    }

    /// <summary>Finds an element by id below <paramref name="parent"/>. The result belongs to a new document.</summary>
    public static HtmlNode? GetElementById(HtmlNode parent, string elementId)
    {
        var tempDocument = new HtmlDocument();
        tempDocument.LoadHtml(parent.OuterHtml);
        return tempDocument.GetElementbyId(elementId);
    }

    public static List<HtmlNode> GetElementsByAttributeValue(HtmlNode parent, string attribute, string value, bool recursive)
    {
        List<HtmlNode> matching = [];
        if (recursive)
        {
            HtmlNodeCollection? nodes = parent.SelectNodes($".//*[@{attribute}='{value}']");
            if (nodes is not null)
            {
                matching.AddRange(nodes);
            }
        }
        else
        {
            foreach (HtmlNode child in parent.ChildNodes)
            {
                if (child.Attributes.Contains(attribute) && child.Attributes[attribute].Value == value)
                {
                    matching.Add(child);
                }
            }
        }

        return matching;
    }

    public static List<HtmlNode> GetElementsByClassName(HtmlNode parent, string className, bool recursive)
    {
        IEnumerable<HtmlNode> children = recursive ? parent.Descendants() : parent.ChildNodes;
        List<HtmlNode> matching = [];
        foreach (HtmlNode child in children)
        {
            if (!child.Attributes.Contains("class"))
            {
                continue;
            }

            string[] parts = child.Attributes["class"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Contains(className))
            {
                matching.Add(child);
            }
        }

        return matching;
    }

    public static List<HtmlNode> GetElementsByTagName(HtmlNode parent, string tag, bool recursive)
    {
        IEnumerable<HtmlNode> nodes = recursive
            ? parent.Descendants(tag)
            : parent.ChildNodes.Where(static node => node.Name is not null).Where(node => node.Name == tag);
        return nodes.ToList();
    }
}

/// <summary>XML node queries shared by the Grasshopper 1 and Grasshopper 2 shells.</summary>
internal static class XmlQueries
{
    /// <summary>Element children plus non-blank text children.</summary>
    public static List<XmlNode> GetChildNodes(XmlNode parent)
    {
        List<XmlNode> children = [];
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element || (child.NodeType == XmlNodeType.Text && !string.IsNullOrWhiteSpace(child.Value)))
            {
                children.Add(child);
            }
        }

        return children;
    }

    public static List<XmlNode> GetElementsByTag(XmlNode parent, string tagName)
    {
        XmlNodeList? elements = parent is XmlElement element
            ? element.GetElementsByTagName(tagName)
            : parent.SelectNodes($".//{tagName}");

        List<XmlNode> results = [];
        if (elements is not null)
        {
            foreach (XmlNode child in elements)
            {
                results.Add(child);
            }
        }

        return results;
    }

    /// <summary>
    /// Builds a new element in its own document. Attribute names and values must have equal counts;
    /// blank attribute names are skipped and reported through <paramref name="skippedAttributeIndices"/>.
    /// </summary>
    public static XmlElement CreateElement(
        string name,
        string? text,
        string? xmlNamespace,
        IReadOnlyList<string> attributeNames,
        IReadOnlyList<string?> attributeValues,
        IEnumerable<XmlNode?> children,
        out List<int> skippedAttributeIndices)
    {
        skippedAttributeIndices = [];

        var document = new XmlDocument();
        XmlElement element = string.IsNullOrWhiteSpace(xmlNamespace)
            ? document.CreateElement(name)
            : document.CreateElement(name, xmlNamespace);

        if (!string.IsNullOrEmpty(text))
        {
            element.InnerText = text;
        }

        for (int index = 0; index < attributeNames.Count; index++)
        {
            string attributeName = attributeNames[index];
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                skippedAttributeIndices.Add(index);
                continue;
            }

            element.SetAttribute(attributeName, attributeValues[index] ?? string.Empty);
        }

        foreach (XmlNode? child in children)
        {
            if (child is null)
            {
                continue;
            }

            // The destination document is private to this call. Only the current
            // source is shared: never hold one source lock while acquiring another,
            // since parallel callers can supply the same documents in reverse order.
            XmlNode imported;
            lock ((object?)child.OwnerDocument ?? child)
                imported = document.ImportNode(child, deep: true);
            element.AppendChild(imported);
        }

        return element;
    }

    public static string ToXmlString(XmlNode? node, bool indent = true)
    {
        if (node is null)
        {
            return string.Empty;
        }

        if (!indent)
        {
            return node.OuterXml;
        }

        var builder = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = true,
        };

        using XmlWriter writer = XmlWriter.Create(builder, settings);
        node.WriteTo(writer);
        writer.Flush();
        return builder.ToString();
    }
}
