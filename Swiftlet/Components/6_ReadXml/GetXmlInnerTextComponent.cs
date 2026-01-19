using System;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GetXmlInnerTextComponent : GH_Component
    {
        public GetXmlInnerTextComponent()
          : base("Get XML Inner Text", "XMLTEXT",
              "Get the inner text content of an XML element",
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
            pManager.AddTextParameter("InnerText", "T", "Inner text content", GH_ParamAccess.item);
            pManager.AddTextParameter("InnerXml", "X", "Inner XML markup", GH_ParamAccess.item);
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
            DA.SetData(0, node.InnerText);
            DA.SetData(1, node.InnerXml);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_get_xml_inner_text;

        public override Guid ComponentGuid => new Guid("F6A7B8C9-D0E1-9F00-4B5C-6D7E8F900112");
    }
}
