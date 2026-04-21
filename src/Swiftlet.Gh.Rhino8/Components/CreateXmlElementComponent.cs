using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateXmlElementComponent : GH_Component
{
    public CreateXmlElementComponent()
        : base("Create XML Element", "CXE", "Create an XML element with optional attributes and child elements", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "N", "Element tag name", GH_ParamAccess.item);
        pManager.AddTextParameter("Text", "T", "Inner text content", GH_ParamAccess.item);
        pManager.AddTextParameter("Namespace", "NS", "XML namespace URI", GH_ParamAccess.item);
        pManager.AddTextParameter("Attr Names", "AN", "List of attribute names", GH_ParamAccess.list);
        pManager.AddTextParameter("Attr Values", "AV", "List of attribute values", GH_ParamAccess.list);
        pManager.AddParameter(new XmlNodeParam(), "Children", "C", "Child XML elements", GH_ParamAccess.list);

        pManager[1].Optional = true;
        pManager[2].Optional = true;
        pManager[3].Optional = true;
        pManager[4].Optional = true;
        pManager[5].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Element", "E", "Created XML element", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string name = string.Empty;
        string text = null;
        string xmlNamespace = null;
        List<string> attributeNames = [];
        List<string> attributeValues = [];
        List<XmlNodeGoo> children = [];

        if (!DA.GetData(0, ref name) || string.IsNullOrWhiteSpace(name))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Element name cannot be empty");
            return;
        }

        DA.GetData(1, ref text);
        DA.GetData(2, ref xmlNamespace);
        DA.GetDataList(3, attributeNames);
        DA.GetDataList(4, attributeValues);
        DA.GetDataList(5, children);

        if (attributeNames.Count != attributeValues.Count)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Attribute names and values must have the same count");
            return;
        }

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
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Skipping attribute at index {index}: name is empty");
                continue;
            }

            element.SetAttribute(attributeName, attributeValues[index] ?? string.Empty);
        }

        foreach (XmlNodeGoo? child in children)
        {
            if (child?.Value is null)
            {
                continue;
            }

            XmlNode importedChild = document.ImportNode(child.Value, deep: true);
            element.AppendChild(importedChild);
        }

        DA.SetData(0, new XmlNodeGoo(element));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("F7E8D9C0-B1A2-4C3D-8E5F-6A7B8C9D0E1F");
}
