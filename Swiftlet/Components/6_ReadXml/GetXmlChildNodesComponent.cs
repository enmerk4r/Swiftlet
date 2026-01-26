using System;
using System.Collections.Generic;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GetXmlChildNodesComponent : GH_Component
    {
        public GetXmlChildNodesComponent()
          : base("Get XML Child Nodes", "XMLCHILDREN",
              "Get the child nodes of an XML element",
              NamingUtility.CATEGORY, NamingUtility.READ_XML)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Parent", "P", "Parent XML element", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Children", "C", "Child nodes", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            XmlNodeGoo goo = null;
            DA.GetData(0, ref goo);

            if (goo == null || goo.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Parent is null");
                return;
            }

            XmlNode node = goo.Value;
            List<XmlNodeGoo> children = new List<XmlNodeGoo>();

            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    // Skip whitespace text nodes
                    if (child.NodeType == XmlNodeType.Element ||
                        (child.NodeType == XmlNodeType.Text && !string.IsNullOrWhiteSpace(child.Value)))
                    {
                        children.Add(new XmlNodeGoo(child));
                    }
                }
            }

            DA.SetDataList(0, children);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_get_xml_child_nodes;

        public override Guid ComponentGuid => new Guid("A7B8C9D0-E1F2-0011-5C6D-7E8F90011223");
    }
}
