using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class QueryParamGoo : GH_Goo<QueryParam>
    {
        public override bool IsValid => this.Value != null && !string.IsNullOrEmpty(this.Value.Key);

        public override string TypeName => "Query Param";

        public override string TypeDescription => "A query param for an Http Web Request";

        public QueryParamGoo()
        {
            this.Value = new QueryParam(null, null);
        }

        public QueryParamGoo(string key, string value)
        {
            this.Value = new QueryParam(key, value);
        }

        public override IGH_Goo Duplicate()
        {
            return new QueryParamGoo(this.Value.Key, this.Value.Value);
        }

        public override string ToString()
        {
            return $"PARAM [ {this.Value.Key} | {this.Value.Value} ]";
        }
    }
}
