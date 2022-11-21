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
    public class ListenerContextParam : GH_Param<ListenerContextGoo>
    {
        public ListenerContextParam()
            : base("Listener Context", "LC", "Collection of Http Request and Response objects",
                 NamingUtility.CATEGORY, NamingUtility.LISTEN, GH_ParamAccess.item)
        {

        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("c8ede657-11f0-460e-9485-68b507d7668d");

        protected override Bitmap Icon => null;
    }
}
