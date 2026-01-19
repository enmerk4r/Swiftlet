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
    public class QueryParamParam : GH_Param<QueryParamGoo>
    {
        public QueryParamParam()
            : base("Query Param", "QP",
                  "Container for Http Query Params",
                  NamingUtility.CATEGORY, NamingUtility.REQUEST, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.senary;
        public override Guid ComponentGuid => new Guid("f59b2397-a996-48b3-8aa3-6b8d56f6244f");

        protected override Bitmap Icon => Properties.Resources.Icons_query_param_24x24;
    }
}
