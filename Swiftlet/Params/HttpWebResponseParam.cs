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
    public class HttpWebResponseParam : GH_Param<HttpWebResponseGoo>
    {
        public HttpWebResponseParam()
            : base("Http Response", "HR", "Http Web Response",
                 NamingUtility.CATEGORY, NamingUtility.REQUEST, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("b1c8d5fd-72ad-4f80-9ada-5f3850bc1a94");

        protected override Bitmap Icon => Properties.Resources.Icons_http_response_param_24x24;
    }
}
