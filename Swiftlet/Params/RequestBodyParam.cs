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
    public class RequestBodyParam : GH_Param<RequestBodyGoo>
    {
        public RequestBodyParam()
            : base("Request Body", "RB", "Collection of Request Body objects",
                 NamingUtility.CATEGORY, NamingUtility.REQUEST, GH_ParamAccess.item)
        {

        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("cc4b9260-48dc-432e-80c8-5ebf9ab3f66e");
    }
}
