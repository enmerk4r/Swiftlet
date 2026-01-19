using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Drawing;

namespace Swiftlet.Params
{
    public class XmlNodeParam : GH_Param<XmlNodeGoo>
    {
        public XmlNodeParam()
            : base("XML Node", "XML", "Collection of XML Nodes",
                 NamingUtility.CATEGORY, NamingUtility.READ_XML, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("3bc83348-4829-5cdc-bffd-80db24d0fbe2");

        protected override Bitmap Icon => Properties.Resources.Icons_xml_node;
    }
}
