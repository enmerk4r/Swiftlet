using System;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class ParseXmlComponent : GH_Component
    {
        public ParseXmlComponent()
          : base("Parse XML", "XML",
              "Parse XML markup into a queryable XML document",
              NamingUtility.CATEGORY, NamingUtility.READ_XML)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("XML", "X", "XML markup string", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Document", "D", "XML Document root node", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string xml = string.Empty;
            DA.GetData(0, ref xml);

            if (string.IsNullOrEmpty(xml))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "XML input is empty");
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                DA.SetData(0, new XmlNodeGoo(doc.DocumentElement));
            }
            catch (XmlException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"XML parsing error: {ex.Message}");
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_parse_xml;

        public override Guid ComponentGuid => new Guid("A1B2C3D4-E5F6-4A5B-9C0D-1E2F3A4B5C6D");
    }
}
