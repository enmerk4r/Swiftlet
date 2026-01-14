using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GetXmlElementsByTagComponent : GH_Component
    {
        public GetXmlElementsByTagComponent()
          : base("Get XML Elements By Tag", "BYTAG",
              "Get XML elements by tag name",
              NamingUtility.CATEGORY, NamingUtility.READ_XML)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Parent", "P", "Parent XML node", GH_ParamAccess.item);
            pManager.AddTextParameter("Tag", "T", "Tag name to search for", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Elements", "E", "Matching XML elements", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            XmlNodeGoo goo = null;
            string tagName = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref tagName);

            if (goo == null || goo.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Parent node is null");
                return;
            }

            if (string.IsNullOrEmpty(tagName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Tag name is empty");
                return;
            }

            XmlNode node = goo.Value;
            List<XmlNodeGoo> results = new List<XmlNodeGoo>();

            // If it's an XmlDocument, get from document element
            XmlNodeList elements;
            if (node is XmlElement element)
            {
                elements = element.GetElementsByTagName(tagName);
            }
            else if (node.OwnerDocument != null)
            {
                elements = node.SelectNodes($".//{tagName}");
            }
            else
            {
                elements = node.SelectNodes($".//{tagName}");
            }

            if (elements != null)
            {
                foreach (XmlNode child in elements)
                {
                    results.Add(new XmlNodeGoo(child));
                }
            }

            DA.SetDataList(0, results);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("B2C3D4E5-F6A7-5B6C-0D1E-2F3A4B5C6D7E");
    }
}
