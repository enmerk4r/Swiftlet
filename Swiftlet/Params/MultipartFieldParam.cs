using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Drawing;

namespace Swiftlet.Params
{
    public class MultipartFieldParam : GH_Param<MultipartFieldGoo>
    {
        public MultipartFieldParam()
            : base("Multipart Field", "MF",
                "Collection of multipart/form-data fields",
                NamingUtility.CATEGORY, NamingUtility.REQUEST, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.senary;
        public override Guid ComponentGuid => new Guid("f5e60c2f-7d03-4a68-b117-848005d73bbc");

        protected override Bitmap Icon => Properties.Resources.Icons_request_body_param_24x24;
    }
}
