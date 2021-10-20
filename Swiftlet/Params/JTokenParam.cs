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
    public class JTokenParam : GH_Param<JTokenGoo>
    {
        public JTokenParam()
            : base("JToken", "JToken", "Collection of JSON Tokens",
                 NamingUtility.CATEGORY, NamingUtility.READ, GH_ParamAccess.item)
        {

        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("a2bdb76c-7a0e-4537-a7f1-0fb37a6b35ac");
    }
}
