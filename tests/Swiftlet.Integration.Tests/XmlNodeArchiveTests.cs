using System.Xml;
using Swiftlet.Runtime;

/// <summary>
/// The text a GH2 XML Node parameter stores (XmlNodeTypeAssistant.Write/Read go through XmlNodeArchive).
/// Every kind of node an XPath or child-node query can output must come back as the same kind with the
/// same content, and what earlier versions stored must still load.
/// </summary>
internal static class XmlNodeArchiveTests
{
    private const string Source =
        "<p:root xmlns:p=\"urn:p\" a=\"1&amp;&quot;2\" p:b=\"x\">hello &lt;world&gt;<!-- note --><![CDATA[x < y]]><p:kid c=\"3\">t</p:kid><?pi some data?></p:root>";

    public static Task RunAsync()
    {
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(Source);
        XmlElement root = document.DocumentElement!;

        XmlNode element = RoundTrip(root.SelectSingleNode("*")!);
        Assert.Equal(XmlNodeType.Element, element.NodeType);
        Assert.Equal("p:kid", element.Name);
        Assert.Equal("urn:p", element.NamespaceURI);
        Assert.Equal("3", ((XmlElement)element).GetAttribute("c"));
        Assert.True(ReferenceEquals(element, element.OwnerDocument!.DocumentElement), "a saved element comes back as the root of its document");

        XmlNode text = RoundTrip(root.SelectSingleNode("text()")!);
        Assert.Equal(XmlNodeType.Text, text.NodeType);
        Assert.Equal("hello <world>", text.Value);

        var attribute = (XmlAttribute)RoundTrip(root.Attributes["a"]!);
        Assert.Equal("a", attribute.Name);
        Assert.Equal("1&\"2", attribute.Value);

        var prefixed = (XmlAttribute)RoundTrip(root.SelectSingleNode("@*[local-name()='b']")!);
        Assert.Equal("p:b", prefixed.Name);
        Assert.Equal("urn:p", prefixed.NamespaceURI);
        Assert.Equal("x", prefixed.Value);

        var declaration = (XmlAttribute)RoundTrip(root.Attributes["xmlns:p"]!);
        Assert.Equal("urn:p", declaration.Value);

        XmlNode comment = RoundTrip(root.SelectSingleNode("comment()")!);
        Assert.Equal(XmlNodeType.Comment, comment.NodeType);
        Assert.Equal(" note ", comment.Value);

        XmlNode cdata = RoundTrip(FirstOf(root, XmlNodeType.CDATA));
        Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        Assert.Equal("x < y", cdata.Value);

        var instruction = (XmlProcessingInstruction)RoundTrip(root.SelectSingleNode("processing-instruction()")!);
        Assert.Equal("pi", instruction.Target);
        Assert.Equal("some data", instruction.Data);

        // Values with line breaks, tabs and surrounding spaces come back exactly.
        XmlNode multiline = RoundTrip(document.CreateTextNode("  a\r\n\tb\n "));
        Assert.Equal("  a\r\n\tb\n ", multiline.Value);
        var multilineAttribute = document.CreateAttribute("m");
        multilineAttribute.Value = "a\r\nb\tc";
        Assert.Equal("a\r\nb\tc", RoundTrip(multilineAttribute).Value);
        XmlNode whitespace = RoundTrip(document.CreateWhitespace("\r\n  "));
        Assert.Equal(XmlNodeType.Whitespace, whitespace.NodeType);
        Assert.Equal("\r\n  ", whitespace.Value);

        // A whole document stays a document; a fragment stays a fragment.
        XmlNode savedDocument = RoundTrip(document);
        Assert.Equal(XmlNodeType.Document, savedDocument.NodeType);
        Assert.Equal(root.OuterXml, ((XmlDocument)savedDocument).DocumentElement!.OuterXml);
        XmlDocumentFragment fragment = document.CreateDocumentFragment();
        fragment.InnerXml = "<a/>text<b/>";
        XmlNode savedFragment = RoundTrip(fragment);
        Assert.Equal(XmlNodeType.DocumentFragment, savedFragment.NodeType);
        Assert.Equal("<a />text<b />", savedFragment.InnerXml);

        // Files saved before the kind was stored: an element was its markup, and it still loads the same way.
        // Other nodes were stored as bare markup that did not load at all; recover the ones that can be.
        XmlNode legacyElement = XmlNodeArchive.Decode(root.OuterXml)!;
        Assert.Equal(root.OuterXml, legacyElement.OuterXml);
        Assert.Equal("hello <world>", XmlNodeArchive.Decode("hello &lt;world&gt;")!.Value);
        Assert.Equal(XmlNodeType.CDATA, XmlNodeArchive.Decode("<![CDATA[x < y]]>")!.NodeType);
        Assert.Equal(XmlNodeType.Comment, XmlNodeArchive.Decode("<!-- note -->")!.NodeType);
        Assert.Equal(XmlNodeType.ProcessingInstruction, XmlNodeArchive.Decode("<?pi some data?>")!.NodeType);
        var legacyAttribute = (XmlAttribute)XmlNodeArchive.Decode("a=\"1&amp;&quot;2\"")!;
        Assert.Equal("1&\"2", legacyAttribute.Value);
        Assert.Null(XmlNodeArchive.Decode("<unclosed"));
        Assert.Null(XmlNodeArchive.Decode(null));

        return Task.CompletedTask;
    }

    private static XmlNode RoundTrip(XmlNode node)
    {
        string stored = XmlNodeArchive.Encode(node);
        XmlNode? restored = XmlNodeArchive.Decode(stored);
        Assert.NotNull(restored);
        Assert.Equal(node.NodeType, restored!.NodeType);
        return restored;
    }

    private static XmlNode FirstOf(XmlNode parent, XmlNodeType type)
    {
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.NodeType == type)
            {
                return child;
            }
        }

        throw new InvalidOperationException("no " + type);
    }
}
