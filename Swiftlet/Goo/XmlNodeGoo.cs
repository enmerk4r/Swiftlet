using Grasshopper.Kernel.Types;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Swiftlet.Goo
{
    public class XmlNodeGoo : GH_Goo<XmlNode>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "XML Node";

        public override string TypeDescription => "A queryable XML Node";

        public XmlNodeGoo()
        {
            this.Value = null;
        }

        public XmlNodeGoo(XmlNode node)
        {
            this.Value = node;
        }

        public override IGH_Goo Duplicate()
        {
            return new XmlNodeGoo(this.Value);
        }

        public override string ToString()
        {
            if (this.Value == null) return "Null XML Node";
            return $"XML Node [ {this.Value.Name} ]";
        }

        /// <summary>
        /// Converts the XML node to a formatted XML string.
        /// </summary>
        public string ToXmlString(bool indent = true)
        {
            if (this.Value == null) return string.Empty;

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
                    this.Value.WriteTo(writer);
                }

                return sb.ToString();
            }
            else
            {
                return this.Value.OuterXml;
            }
        }

        public override bool CastTo<Q>(ref Q target)
        {
            Type q = typeof(Q);

            if (q == typeof(GH_String))
            {
                if (this.Value != null)
                {
                    object temp = new GH_String(this.Value.OuterXml);
                    target = (Q)temp;
                    return true;
                }
            }

            return base.CastTo(ref target);
        }
    }
}
