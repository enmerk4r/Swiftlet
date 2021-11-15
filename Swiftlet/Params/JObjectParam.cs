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
    public class JObjectParam : GH_Param<JObjectGoo>
    {
        public JObjectParam()
            :base("JObject", "JO", "Collection of JSON Objects",
                 NamingUtility.CATEGORY, NamingUtility.READ_JSON, GH_ParamAccess.item)
        {

        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("90f38432-a460-43e2-a1ac-747d8ca6236c");

        protected override Bitmap Icon => Properties.Resources.Icons_jobject_param_24x24;
    }
}
