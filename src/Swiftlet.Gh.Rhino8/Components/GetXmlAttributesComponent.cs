using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetXmlAttributesComponent : GH_Component
{
    public GetXmlAttributesComponent()
        : base("Get XML Attributes", "XMLATTRS", "Get all attributes from an XML element as key-value pairs", ShellNaming.Category, ShellNaming.ReadXml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Element", "E", "XML element", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Names", "N", "Attribute names", GH_ParamAccess.list);
        pManager.AddTextParameter("Values", "V", "Attribute values", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        XmlNodeGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element is null");
            return;
        }

        List<string> names = [];
        List<string> values = [];
        if (goo.Value.Attributes is not null)
        {
            foreach (XmlAttribute attribute in goo.Value.Attributes)
            {
                names.Add(attribute.Name);
                values.Add(attribute.Value);
            }
        }

        DA.SetDataList(0, names);
        DA.SetDataList(1, values);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("E5F6A7B8-C9D0-8E9F-3A4B-5C6D7E8F9001");
}

