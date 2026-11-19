using Grasshopper2.Components;
using Grasshopper2.Parameters;
using GrasshopperIO;
using HtmlAgilityPack;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

// "5. Read HTML". HTML nodes are shared between components and solver threads, and HtmlAgilityPack
// builds some collections lazily, so every read happens under a lock on the owning document.

internal static class HtmlSync
{
    public static object For(HtmlNode node) => (object?)node.OwnerDocument ?? node;
}

[IoId(ComponentIds.ReadHtml)]
public sealed class ReadHtmlComponent : SwiftletComponent
{
    public ReadHtmlComponent() : base("Read HTML", "Read HTML Markup", Sections.ReadHtml) { }
    public ReadHtmlComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("HTML", "H", "HTML Markup");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HtmlNodeParameter("Node", "N", "Html Node"));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string html);
        access.SetItem(0, HtmlQueries.Parse(html ?? string.Empty));
    }
}

[IoId(ComponentIds.GetElementById)]
public sealed class GetElementByIdComponent : SwiftletComponent
{
    public GetElementByIdComponent() : base("Get Element By Id", "Get an HTML element by ID", Sections.ReadHtml) { }
    public GetElementByIdComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Parent", "P", "Parent node"));
        inputs.AddText("ID", "I", "HTML Element ID");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HtmlNodeParameter("Element", "E", "Found element"));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out HtmlNode parent);
        access.GetItem(1, out string elementId);
        if (parent is null || string.IsNullOrWhiteSpace(elementId))
            return;

        HtmlNode? element;
        lock (HtmlSync.For(parent))
            element = HtmlQueries.GetElementById(parent, elementId);

        if (element is not null)
            access.SetItem(0, element);
    }
}

[IoId(ComponentIds.GetElementsByAttributeValue)]
public sealed class GetElementsByAttributeValueComponent : SwiftletComponent
{
    public GetElementsByAttributeValueComponent()
        : base("Get Elements By Attribute Value", "Get all HTML elements where a certain attribute is equal to a certain value", Sections.ReadHtml) { }
    public GetElementsByAttributeValueComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Parent", "P", "Parent node"));
        inputs.AddText("Attribute", "A", "Name of the HTML attribute");
        inputs.AddText("Value", "V", "Attribute value to search for");
        inputs.AddBoolean("Recursive", "R", "Determines whether to search for specified attribute in all of the descendants, or only one level down", Access.Item, Requirement.MayBeMissing).Set(true);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HtmlNodeParameter("Children", "C", "A list of child nodes", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out HtmlNode parent);
        access.GetItem(1, out string attribute);
        access.GetItem(2, out string value);
        if (!access.GetItem(3, out bool recursive))
            recursive = true;

        if (parent is null || string.IsNullOrWhiteSpace(attribute))
            return;

        HtmlNode[] matching;
        lock (HtmlSync.For(parent))
            matching = HtmlQueries.GetElementsByAttributeValue(parent, attribute, value ?? string.Empty, recursive).ToArray();

        access.SetTwig(0, matching);
    }
}

[IoId(ComponentIds.GetElementsByClassName)]
public sealed class GetElementsByClassNameComponent : SwiftletComponent
{
    public GetElementsByClassNameComponent()
        : base("Get Elements By Class Name", "Get all HTML elements by a specific class name", Sections.ReadHtml) { }
    public GetElementsByClassNameComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Parent", "P", "Parent node"));
        inputs.AddText("Class", "C", "Name of the HTML class");
        inputs.AddBoolean("Recursive", "R", "Determines whether to search for the specified class in all of the descendants, or only one level down", Access.Item, Requirement.MayBeMissing).Set(true);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HtmlNodeParameter("Children", "C", "A list of child nodes", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out HtmlNode parent);
        access.GetItem(1, out string className);
        if (!access.GetItem(2, out bool recursive))
            recursive = true;

        if (parent is null || string.IsNullOrWhiteSpace(className))
            return;

        HtmlNode[] matching;
        lock (HtmlSync.For(parent))
            matching = HtmlQueries.GetElementsByClassName(parent, className, recursive).ToArray();

        access.SetTwig(0, matching);
    }
}

[IoId(ComponentIds.GetElementsByTagName)]
public sealed class GetElementsByTagNameComponent : SwiftletComponent
{
    public GetElementsByTagNameComponent()
        : base("Get Elements By Tag Name", "Get HTML Elements by Tag Name", Sections.ReadHtml) { }
    public GetElementsByTagNameComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Parent", "P", "Parent node"));
        inputs.AddText("Tag", "T", "Name of the HTML tag");
        inputs.AddBoolean("Recursive", "R", "Determines whether to search for specified tags in all of the descendants, or only one level down", Access.Item, Requirement.MayBeMissing).Set(true);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HtmlNodeParameter("Children", "C", "A list of child nodes", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out HtmlNode parent);
        access.GetItem(1, out string tag);
        if (!access.GetItem(2, out bool recursive))
            recursive = true;

        if (parent is null || string.IsNullOrWhiteSpace(tag))
            return;

        HtmlNode[] matching;
        lock (HtmlSync.For(parent))
            matching = HtmlQueries.GetElementsByTagName(parent, tag, recursive).ToArray();

        access.SetTwig(0, matching);
    }
}

[IoId(ComponentIds.GetElementsByXPath)]
public sealed class GetElementsByXPathComponent : SwiftletComponent
{
    public GetElementsByXPathComponent()
        : base("Get Elements By XPATH", "Get HTML elements via an XPATH expression", Sections.ReadHtml) { }
    public GetElementsByXPathComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Parent", "P", "Parent node"));
        inputs.AddText("XPATH", "X", "XPATH Expression");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HtmlNodeParameter("Children", "C", "A list of child nodes", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out HtmlNode parent);
        access.GetItem(1, out string xpath);
        if (parent is null || string.IsNullOrWhiteSpace(xpath))
            return;

        HtmlNode[]? children;
        try
        {
            lock (HtmlSync.For(parent))
                children = parent.SelectNodes(xpath)?.ToArray();
        }
        catch (System.Xml.XPath.XPathException ex)
        {
            access.AddError("Invalid XPath", ex.Message);
            return;
        }

        if (children is not null)
            access.SetTwig(0, children);
    }
}

[IoId(ComponentIds.GetHtmlAttributeValue)]
public sealed class GetHtmlAttributeValueComponent : SwiftletComponent
{
    public GetHtmlAttributeValueComponent()
        : base("Get Attribute Value", "Get the value of an HTML attribute", Sections.ReadHtml) { }
    public GetHtmlAttributeValueComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Node", "N", "HTML node"));
        inputs.AddText("Attribute", "A", "Name of the HTML attribute");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Value", "V", "HTML Attribute value");
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out HtmlNode node);
        access.GetItem(1, out string attribute);
        if (node is null || string.IsNullOrWhiteSpace(attribute))
            return;

        string? value = null;
        lock (HtmlSync.For(node))
        {
            if (node.Attributes.Contains(attribute))
                value = node.Attributes[attribute].Value;
        }

        if (value is not null)
            access.SetItem(0, value);
    }
}

[IoId(ComponentIds.GetHtmlAttributes)]
public sealed class GetHtmlAttributesComponent : SwiftletComponent
{
    public GetHtmlAttributesComponent()
        : base("Get Html Attributes", "Get all attributes and values of an HTML element", Sections.ReadHtml) { }
    public GetHtmlAttributesComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Node", "N", "HTML node"));
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Names", "N", "List of HTML Attribute names", Access.Twig);
        outputs.AddText("Values", "V", "List of HTML Attribute values", Access.Twig);
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out HtmlNode node) || node is null)
            return;

        string[] names;
        string[] values;
        lock (HtmlSync.For(node))
        {
            names = node.Attributes.Select(static attribute => attribute.Name).ToArray();
            values = node.Attributes.Select(static attribute => attribute.Value).ToArray();
        }

        access.SetTwig(0, names);
        access.SetTwig(1, values);
    }
}

[IoId(ComponentIds.GetHtmlNodeChildren)]
public sealed class GetHtmlNodeChildrenComponent : SwiftletComponent
{
    public GetHtmlNodeChildrenComponent()
        : base("Get Child Nodes", "Get all child nodes of an HTML node", Sections.ReadHtml) { }
    public GetHtmlNodeChildrenComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Parent", "P", "Parent node"));
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new HtmlNodeParameter("Children", "C", "A list of child nodes", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out HtmlNode parent) || parent is null)
            return;

        HtmlNode[] children;
        lock (HtmlSync.For(parent))
            children = parent.ChildNodes.ToArray();

        access.SetTwig(0, children);
    }
}

[IoId(ComponentIds.GetInnerHtml)]
public sealed class GetInnerHtmlComponent : SwiftletComponent
{
    public GetInnerHtmlComponent() : base("Get Inner HTML", "Get the inner HTML of an element", Sections.ReadHtml) { }
    public GetInnerHtmlComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("Node", "N", "HTML node"));
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("HTML", "H", "Inner HTML");
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out HtmlNode node) || node is null)
            return;

        string inner;
        lock (HtmlSync.For(node))
            inner = node.InnerHtml;

        access.SetItem(0, inner);
    }
}

[IoId(ComponentIds.StringifyHtmlNode)]
public sealed class StringifyHtmlNodeComponent : SwiftletComponent
{
    public StringifyHtmlNodeComponent() : base("Stringify HTML Node", "Convert an HTML Node to cleartext", Sections.ReadHtml) { }
    public StringifyHtmlNodeComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HtmlNodeParameter("HTML Node", "N", "HTML Node object"));
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("HTML", "H", "HTML Markup as text");
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out HtmlNode node) || node is null)
            return;

        string outer;
        lock (HtmlSync.For(node))
            outer = node.OuterHtml;

        access.SetItem(0, outer);
    }
}
