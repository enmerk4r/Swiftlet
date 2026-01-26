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
    public class HttpHeaderParam : GH_Param<HttpHeaderGoo>
    {
        public HttpHeaderParam()
            : base("Http Header", "H",
                 "A collection of Http Headers",
                 NamingUtility.CATEGORY, NamingUtility.REQUEST, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.senary;
        public override Guid ComponentGuid => new Guid("521dae84-7074-433e-9714-9144c42b92a4");

        protected override Bitmap Icon => Properties.Resources.Icons_http_header_param_24x24;
    }
}
