using System;
using System.Collections.Generic;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GetXmlAttributesComponent : GH_Component
    {
        public GetXmlAttributesComponent()
          : base("Get XML Attributes", "XMLATTRS",
              "Get all attributes from an XML element as key-value pairs",
              NamingUtility.CATEGORY, NamingUtility.READ_XML)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

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
            XmlNodeGoo goo = null;
            DA.GetData(0, ref goo);

            if (goo == null || goo.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element is null");
                return;
            }

            XmlNode node = goo.Value;
            List<string> names = new List<string>();
            List<string> values = new List<string>();

            if (node.Attributes != null)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    names.Add(attr.Name);
                    values.Add(attr.Value);
                }
            }

            DA.SetDataList(0, names);
            DA.SetDataList(1, values);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_get_xml_attributes;

        public override Guid ComponentGuid => new Guid("E5F6A7B8-C9D0-8E9F-3A4B-5C6D7E8F9001");
    }
}
