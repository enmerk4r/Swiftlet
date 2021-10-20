using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class JObjectGoo : GH_Goo<JObject>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "JSON Object";

        public override string TypeDescription => "Searchable JSON Token";

        public JObjectGoo()
        {
            this.Value = null;
        }

        public JObjectGoo(JObject obj)
        {
            this.Value = obj;
        }

        public override IGH_Goo Duplicate()
        {
            return new JObjectGoo(this.Value.DeepClone() as JObject);
        }

        public override string ToString()
        {
            return "JSON Object";
        }
    }
}
