using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Params
{
    public class HttpWebResponseParam : GH_Param<HttpWebResponseGoo>
    {
        public HttpWebResponseParam()
            : base("Http Response", "Http Response", "Http Web Response",
                 NamingUtility.CATEGORY, NamingUtility.REQUESTS, GH_ParamAccess.item)
        {
        }

        public HttpWebResponseParam(GH_ParamAccess access)
            : base("Http Response", "Http Response", "Http Web Response",
                 NamingUtility.CATEGORY, NamingUtility.REQUESTS, access)
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("b1c8d5fd-72ad-4f80-9ada-5f3850bc1a94");
    }
}
