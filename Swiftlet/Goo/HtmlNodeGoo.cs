using Grasshopper.Kernel.Types;
using HtmlAgilityPack;
using Swiftlet.DataModels.Enums;
using Swiftlet.DataModels.Implementations;
using Swiftlet.DataModels.Interfaces;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class HtmlNodeGoo : GH_Goo<HtmlNode>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "Html Node";

        public override string TypeDescription => "A queriable Html Node";

        public HtmlNodeGoo()
        {
            this.Value = null;
        }

        public HtmlNodeGoo(HtmlNode node)
        {
            string html = node.OuterHtml;
            HtmlDocument dom = new HtmlDocument();
            dom.LoadHtml(html);
            this.Value = dom.DocumentNode;
        }

        public override IGH_Goo Duplicate()
        {
            return new HtmlNodeGoo(this.Value);
        }

        public override string ToString()
        {
            return this.Value.OuterHtml;
        }
    }
}
