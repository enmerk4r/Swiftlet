using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetXmlAttributeValueComponent : GH_Component
{
    public GetXmlAttributeValueComponent()
        : base("Get XML Attribute Value", "XMLATTR", "Get the value of an attribute from an XML element", ShellNaming.Category, ShellNaming.ReadXml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Element", "E", "XML element", GH_ParamAccess.item);
        pManager.AddTextParameter("Attribute", "A", "Attribute name", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Value", "V", "Attribute value", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        XmlNodeGoo? goo = null;
        string attributeName = string.Empty;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref attributeName);

        if (goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(attributeName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Attribute name is empty");
            return;
        }

        if (goo.Value.Attributes is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element has no attributes");
            return;
        }

        XmlAttribute? attribute = goo.Value.Attributes[attributeName];
        if (attribute is not null)
        {
            DA.SetData(0, attribute.Value);
        }
        else
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Attribute '{attributeName}' not found");
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("D4E5F6A7-B8C9-7D8E-2F3A-4B5C6D7E8F90");
}

