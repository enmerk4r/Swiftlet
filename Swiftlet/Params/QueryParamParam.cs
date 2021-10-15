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
    public class QueryParamParam : GH_Param<QueryParamGoo>
    {
        public QueryParamParam()
            : base("Query Param", "Query Param",
                  "Container for Http Query Params",
                  NamingUtility.CATEGORY, NamingUtility.PARAMS, GH_ParamAccess.item)
        {
        }

        public QueryParamParam(GH_ParamAccess access)
            : base("Query Param", "Query Param",
                  "Container for Http Query Params",
                  NamingUtility.CATEGORY, NamingUtility.PARAMS, access)
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("f59b2397-a996-48b3-8aa3-6b8d56f6244f");
    }
}
