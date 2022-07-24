using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Params
{
    public class HtmlNodeParam : GH_Param<HtmlNodeGoo>
    {
        public HtmlNodeParam()
            : base("Html Node", "HTML", "Collection of Html Nodes",
                 NamingUtility.CATEGORY, NamingUtility.READ_HTML, GH_ParamAccess.item)
        {

        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("2ac72237-3718-4bcb-aefc-79ca13c9fad1");

        protected override Bitmap Icon => null;
    }
}
