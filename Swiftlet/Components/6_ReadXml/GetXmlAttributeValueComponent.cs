using System;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GetXmlAttributeValueComponent : GH_Component
    {
        public GetXmlAttributeValueComponent()
          : base("Get XML Attribute Value", "XMLATTR",
              "Get the value of an attribute from an XML element",
              NamingUtility.CATEGORY, NamingUtility.READ_XML)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

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
            XmlNodeGoo goo = null;
            string attrName = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref attrName);

            if (goo == null || goo.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element is null");
                return;
            }

            if (string.IsNullOrEmpty(attrName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Attribute name is empty");
                return;
            }

            XmlNode node = goo.Value;
            if (node.Attributes == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element has no attributes");
                return;
            }

            XmlAttribute attr = node.Attributes[attrName];
            if (attr != null)
            {
                DA.SetData(0, attr.Value);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Attribute '{attrName}' not found");
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_get_xml_attribute_value;

        public override Guid ComponentGuid => new Guid("D4E5F6A7-B8C9-7D8E-2F3A-4B5C6D7E8F90");
    }
}
