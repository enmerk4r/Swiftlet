using System.Xml;
using System.Xml.XPath;
using Grasshopper2.Components;
using Grasshopper2.Parameters;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

// "6. Read XML" (plus Create XML Element from "2. Request"). XML nodes are shared between components
// and solver threads; XmlDocument is not thread-safe even for reads, so every access happens under a
// lock on the owning document.

internal static class XmlSync
{
    public static object For(XmlNode node) => (object?)node.OwnerDocument ?? node;
}

[IoId(ComponentIds.ParseXml)]
public sealed class ParseXmlComponent : SwiftletComponent
{
    public ParseXmlComponent() : base("Parse XML", "Parse XML markup into a queryable XML document", Sections.ReadXml) { }
    public ParseXmlComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("XML", "Xm", "XML markup string");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new XmlNodeParameter("Document", "Dc", "XML Document root node"));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string xml);
        if (string.IsNullOrWhiteSpace(xml))
        {
            access.AddWarning("Empty input", "XML input is empty");
            return;
        }

        try
        {
            var document = new XmlDocument();
            document.LoadXml(xml);
            if (document.DocumentElement is not null)
                access.SetItem(0, document.DocumentElement);
        }
        catch (XmlException ex)
        {
            access.AddError("XML parsing error", ex.Message);
        }
    }
}

[IoId(ComponentIds.CreateXmlElement)]
public sealed class CreateXmlElementComponent : SwiftletComponent
{
    public CreateXmlElementComponent()
        : base("Create XML Element", "Create an XML element with optional attributes and child elements", Sections.Request) { }
    public CreateXmlElementComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Name", "Nm", "Element tag name");
        inputs.AddText("Text", "Tx", "Inner text content", Access.Item, Requirement.MayBeMissing);
        inputs.AddText("Namespace", "Ns", "XML namespace URI", Access.Item, Requirement.MayBeMissing);

        // Optional lists need an explicit empty twig or the component does not run when they are unconnected.
        inputs.AddText("Attr Names", "An", "List of attribute names", Access.Twig, Requirement.MayBeMissing).Set(Array.Empty<string>());
        inputs.AddText("Attr Values", "Av", "List of attribute values", Access.Twig, Requirement.MayBeMissing).Set(Array.Empty<string>());
        var children = new XmlNodeParameter("Children", "Ch", "Child XML elements", Access.Twig);
        children.Set(Array.Empty<XmlNode>());
        inputs.Add(children, Requirement.MayBeMissing);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new XmlNodeParameter("Element", "El", "Created XML element"));
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out string name) || string.IsNullOrWhiteSpace(name))
        {
            access.AddError("Missing name", "Element name cannot be empty");
            return;
        }

        access.GetItem(1, out string text);
        access.GetItem(2, out string xmlNamespace);
        access.GetTwig<string>(3, out var attributeNames);
        access.GetTwig<string>(4, out var attributeValues);
        access.GetTwig<XmlNode>(5, out var children);

        string[] names = attributeNames?.NonNullItems.ToArray() ?? Array.Empty<string>();
        string[] values = attributeValues?.NonNullItems.ToArray() ?? Array.Empty<string>();
        XmlNode[] childNodes = children?.NonNullItems.ToArray() ?? Array.Empty<XmlNode>();

        if (names.Length != values.Length)
        {
            access.AddError("Attribute mismatch", "Attribute names and values must have the same count");
            return;
        }

        // Importing a child reads its source document, so hold that document's lock while importing.
        XmlElement element;
        List<int> skipped;
        try
        {
            element = CreateLocked(name, text, xmlNamespace, names, values, childNodes, 0, out skipped);
        }
        catch (XmlException ex)
        {
            access.AddError("Invalid XML name", ex.Message);
            return;
        }

        foreach (int index in skipped)
            access.AddWarning("Attribute skipped", $"Skipping attribute at index {index}: name is empty");

        access.SetItem(0, element);
    }

    private static XmlElement CreateLocked(
        string name, string? text, string? xmlNamespace, string[] names, string[] values, XmlNode[] children, int lockIndex, out List<int> skipped)
    {
        if (lockIndex >= children.Length)
            return XmlQueries.CreateElement(name, text, xmlNamespace, names, values, children, out skipped);

        lock (XmlSync.For(children[lockIndex]))
            return CreateLocked(name, text, xmlNamespace, names, values, children, lockIndex + 1, out skipped);
    }
}

[IoId(ComponentIds.GetXmlAttributeValue)]
public sealed class GetXmlAttributeValueComponent : SwiftletComponent
{
    public GetXmlAttributeValueComponent()
        : base("Get XML Attribute Value", "Get the value of an attribute from an XML element", Sections.ReadXml) { }
    public GetXmlAttributeValueComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new XmlNodeParameter("Element", "El", "XML element"));
        inputs.AddText("Attribute", "At", "Attribute name");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Value", "Vl", "Attribute value");
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out XmlNode element);
        access.GetItem(1, out string attributeName);

        if (element is null)
        {
            access.AddWarning("Missing element", "Element is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(attributeName))
        {
            access.AddWarning("Missing attribute name", "Attribute name is empty");
            return;
        }

        lock (XmlSync.For(element))
        {
            if (element.Attributes is null)
            {
                access.AddWarning("No attributes", "Element has no attributes");
                return;
            }

            XmlAttribute? attribute = element.Attributes[attributeName];
            if (attribute is not null)
                access.SetItem(0, attribute.Value);
            else
                access.AddWarning("Attribute not found", $"Attribute '{attributeName}' not found");
        }
    }
}

[IoId(ComponentIds.GetXmlAttributes)]
public sealed class GetXmlAttributesComponent : SwiftletComponent
{
    public GetXmlAttributesComponent()
        : base("Get XML Attributes", "Get all attributes from an XML element as key-value pairs", Sections.ReadXml) { }
    public GetXmlAttributesComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new XmlNodeParameter("Element", "El", "XML element"));
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Names", "Nm", "Attribute names", Access.Twig);
        outputs.AddText("Values", "Vl", "Attribute values", Access.Twig);
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out XmlNode element) || element is null)
        {
            access.AddWarning("Missing element", "Element is null");
            return;
        }

        List<string> names = [];
        List<string> values = [];
        lock (XmlSync.For(element))
        {
            if (element.Attributes is not null)
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    names.Add(attribute.Name);
                    values.Add(attribute.Value);
                }
            }
        }

        access.SetTwig(0, names.ToArray());
        access.SetTwig(1, values.ToArray());
    }
}

[IoId(ComponentIds.GetXmlChildNodes)]
public sealed class GetXmlChildNodesComponent : SwiftletComponent
{
    public GetXmlChildNodesComponent()
        : base("Get XML Child Nodes", "Get the child nodes of an XML element", Sections.ReadXml) { }
    public GetXmlChildNodesComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new XmlNodeParameter("Parent", "Pr", "Parent XML element"));
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new XmlNodeParameter("Children", "Ch", "Child nodes", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out XmlNode parent) || parent is null)
        {
            access.AddWarning("Missing parent", "Parent is null");
            return;
        }

        XmlNode[] children;
        lock (XmlSync.For(parent))
            children = XmlQueries.GetChildNodes(parent).ToArray();

        access.SetTwig(0, children);
    }
}

[IoId(ComponentIds.GetXmlElementsByTag)]
public sealed class GetXmlElementsByTagComponent : SwiftletComponent
{
    public GetXmlElementsByTagComponent()
        : base("Get XML Elements By Tag", "Get XML elements by tag name", Sections.ReadXml) { }
    public GetXmlElementsByTagComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new XmlNodeParameter("Parent", "Pr", "Parent XML node"));
        inputs.AddText("Tag", "Tg", "Tag name to search for");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new XmlNodeParameter("Elements", "El", "Matching XML elements", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out XmlNode parent);
        access.GetItem(1, out string tagName);

        if (parent is null)
        {
            access.AddWarning("Missing parent", "Parent node is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(tagName))
        {
            access.AddWarning("Missing tag", "Tag name is empty");
            return;
        }

        XmlNode[] results;
        try
        {
            lock (XmlSync.For(parent))
                results = XmlQueries.GetElementsByTag(parent, tagName).ToArray();
        }
        catch (XPathException ex)
        {
            access.AddError("Invalid tag name", ex.Message);
            return;
        }

        access.SetTwig(0, results);
    }
}

[IoId(ComponentIds.GetXmlElementsByXPath)]
public sealed class GetXmlElementsByXPathComponent : SwiftletComponent
{
    public GetXmlElementsByXPathComponent()
        : base("Get XML Elements By XPath", "Query XML elements using an XPath expression", Sections.ReadXml) { }
    public GetXmlElementsByXPathComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new XmlNodeParameter("Parent", "Pr", "Parent XML node"));
        inputs.AddText("XPath", "Xp", "XPath expression");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new XmlNodeParameter("Elements", "El", "Matching XML elements", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out XmlNode parent);
        access.GetItem(1, out string xpath);

        if (parent is null)
        {
            access.AddWarning("Missing parent", "Parent node is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(xpath))
        {
            access.AddWarning("Missing XPath", "XPath expression is empty");
            return;
        }

        try
        {
            XmlNode[]? elements;
            lock (XmlSync.For(parent))
                elements = parent.SelectNodes(xpath)?.Cast<XmlNode>().ToArray();

            if (elements is not null)
                access.SetTwig(0, elements);
        }
        catch (XPathException ex)
        {
            access.AddError("Invalid XPath", ex.Message);
        }
    }
}

[IoId(ComponentIds.GetXmlInnerText)]
public sealed class GetXmlInnerTextComponent : SwiftletComponent
{
    public GetXmlInnerTextComponent()
        : base("Get XML Inner Text", "Get the inner text content of an XML element", Sections.ReadXml) { }
    public GetXmlInnerTextComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new XmlNodeParameter("Element", "El", "XML element"));
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("InnerText", "Tx", "Inner text content");
        outputs.AddText("InnerXml", "Xm", "Inner XML markup");
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out XmlNode element) || element is null)
        {
            access.AddWarning("Missing element", "Element is null");
            return;
        }

        string innerText;
        string innerXml;
        lock (XmlSync.For(element))
        {
            innerText = element.InnerText;
            innerXml = element.InnerXml;
        }

        access.SetItem(0, innerText);
        access.SetItem(1, innerXml);
    }
}

[IoId(ComponentIds.StringifyXmlNode)]
public sealed class StringifyXmlNodeComponent : SwiftletComponent
{
    public StringifyXmlNodeComponent()
        : base("Stringify XML Node", "Convert an XML node back to a formatted XML string", Sections.ReadXml) { }
    public StringifyXmlNodeComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new XmlNodeParameter("Node", "Nd", "XML node to stringify"));
        inputs.AddBoolean("Indent", "In", "Pretty-print with indentation").Set(true);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("XML", "Xm", "XML string");
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out XmlNode node);
        if (!access.GetItem(1, out bool indent))
            indent = true;

        if (node is null)
        {
            access.AddWarning("Missing node", "Node is null");
            return;
        }

        string xml;
        lock (XmlSync.For(node))
            xml = XmlQueries.ToXmlString(node, indent);

        access.SetItem(0, xml);
    }
}
