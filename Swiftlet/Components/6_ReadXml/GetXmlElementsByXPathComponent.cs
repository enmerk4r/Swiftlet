using System;
using System.Collections.Generic;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GetXmlElementsByXPathComponent : GH_Component
    {
        public GetXmlElementsByXPathComponent()
          : base("Get XML Elements By XPath", "BYXPATH",
              "Query XML elements using an XPath expression",
              NamingUtility.CATEGORY, NamingUtility.READ_XML)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Parent", "P", "Parent XML node", GH_ParamAccess.item);
            pManager.AddTextParameter("XPath", "X", "XPath expression", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Elements", "E", "Matching XML elements", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            XmlNodeGoo goo = null;
            string xpath = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref xpath);

            if (goo == null || goo.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Parent node is null");
                return;
            }

            if (string.IsNullOrEmpty(xpath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "XPath expression is empty");
                return;
            }

            XmlNode node = goo.Value;
            List<XmlNodeGoo> results = new List<XmlNodeGoo>();

            try
            {
                XmlNodeList elements = node.SelectNodes(xpath);
                if (elements != null)
                {
                    foreach (XmlNode child in elements)
                    {
                        results.Add(new XmlNodeGoo(child));
                    }
                }
            }
            catch (System.Xml.XPath.XPathException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Invalid XPath: {ex.Message}");
            }

            DA.SetDataList(0, results);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("C3D4E5F6-A7B8-6C7D-1E2F-3A4B5C6D7E8F");
    }
}
