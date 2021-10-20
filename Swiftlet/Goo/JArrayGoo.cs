using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class JArrayGoo : GH_Goo<JArray>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "JSON Array";

        public override string TypeDescription => "JSON Array Object";

        public JArrayGoo()
        {
            this.Value = null;
        }

        public JArrayGoo(JArray array)
        {
            this.Value = array;
        }

        public override IGH_Goo Duplicate()
        {
            return new JArrayGoo(this.Value.DeepClone() as JArray);
        }

        public override string ToString()
        {
            return "JSON Array";
        }
    }
}
