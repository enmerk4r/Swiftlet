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
    public class ByteArrayParam : GH_Param<ByteArrayGoo>
    {
        public ByteArrayParam()
            : base("Byte Array", "BA", "Collection of Byte Arrays",
                 NamingUtility.CATEGORY, NamingUtility.UTILITIES, GH_ParamAccess.item)
        {

        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("8c021fd2-744d-4f13-bbe4-94467cf397a1");

        protected override Bitmap Icon => null;
    }
}
