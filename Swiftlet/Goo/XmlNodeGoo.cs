using Grasshopper.Kernel.Types;
using System;
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
    }
}
