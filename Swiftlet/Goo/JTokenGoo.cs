using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class JTokenGoo : GH_Goo<JToken>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "JSON Token";

        public override string TypeDescription => "Abstract JSON Token";

        public JTokenGoo()
        {
            this.Value = null;
        }

        public JTokenGoo(JToken token)
        {
            this.Value = token;
        }

        public override IGH_Goo Duplicate()
        {
            return new JTokenGoo(this.Value.DeepClone());
        }

        public override string ToString()
        {
            return "JSON Token";
        }
    }
}
