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
    public class JValueParam : GH_Param<JValueGoo>
    {
        public JValueParam()
            :base("JValue", "JV", "Collection of JSON Values",
                 NamingUtility.CATEGORY, NamingUtility.READ_JSON, GH_ParamAccess.item)
        {

        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("f46d2a50-02f2-46c9-8a93-f7a8d37843d4");
    }
}
