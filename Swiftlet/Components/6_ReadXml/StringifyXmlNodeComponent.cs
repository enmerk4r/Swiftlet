using System;
using System.IO;
using System.Text;
using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class StringifyXmlNodeComponent : GH_Component
    {
        public StringifyXmlNodeComponent()
          : base("Stringify XML Node", "XML2STR",
              "Convert an XML node back to a formatted XML string",
              NamingUtility.CATEGORY, NamingUtility.READ_XML)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new XmlNodeParam(), "Node", "N", "XML node to stringify", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Indent", "I", "Pretty-print with indentation", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("XML", "X", "XML string", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            XmlNodeGoo goo = null;
            bool indent = true;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref indent);

            if (goo == null || goo.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node is null");
                return;
            }

            XmlNode node = goo.Value;

            if (indent)
            {
                StringBuilder sb = new StringBuilder();
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = true
                };

                using (XmlWriter writer = XmlWriter.Create(sb, settings))
                {
                    node.WriteTo(writer);
                }

                DA.SetData(0, sb.ToString());
            }
            else
            {
                DA.SetData(0, node.OuterXml);
            }
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("B8C9D0E1-F2A3-1122-6D7E-8F9001122334");
    }
}
