using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class JValueGoo : GH_Goo<JValue>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "JSON Value";

        public override string TypeDescription => "JSON Value Object";

        public JValueGoo()
        {
            this.Value = null;
        }

        public JValueGoo(JValue value)
        {
            this.Value = value;
        }

        public override IGH_Goo Duplicate()
        {
            return new JValueGoo(this.Value.DeepClone() as JValue);
        }

        public override string ToString()
        {
            return "JSON Value";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            Type q = typeof(Q);
            JValue value = this.Value;
            if (q == typeof(JTokenGoo))
            {
                JToken token = value;
                if (token != null)
                {
                    object temp = new JTokenGoo(token);
                    target = (Q)temp;
                    return true;
                }
            }
            return base.CastTo(ref target);
        }

        public override bool CastFrom(object source)
        {
            if (source != null)
            {
                if (source.GetType() == typeof(JTokenGoo))
                {
                    JTokenGoo goo = source as JTokenGoo;
                    if (goo != null)
                    {
                        JValue value = goo.Value as JValue;
                        if (value != null)
                        {
                            this.Value = value;
                            return true;
                        }
                    }
                }
            }
            return base.CastFrom(source);
        }
    }
}
