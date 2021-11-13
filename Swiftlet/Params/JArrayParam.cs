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
    public class JArrayParam : GH_Param<JArrayGoo>
    {
        public JArrayParam()
            : base("JArray", "JA", "Collection of JSON Arrays",
                  NamingUtility.CATEGORY, NamingUtility.READ_JSON, GH_ParamAccess.item)
        {

        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("645a1994-d22d-4103-877e-849f96811291");
    }
}
