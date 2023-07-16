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
    public class BitmapParam : GH_Param<BitmapGoo>
    {
        public BitmapParam()
            : base("Bitmap", "BMP", "Collection of Bitmaps",
                 NamingUtility.CATEGORY, NamingUtility.UTILITIES, GH_ParamAccess.item)
        {

        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("c1cdd3a5-faaf-484a-978b-307139ebf7f4");

        protected override Bitmap Icon => Properties.Resources.Icons_bitmap_24x24;
    }
}
