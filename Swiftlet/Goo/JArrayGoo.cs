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
            if (this.Value == null)
            {
                return "JSON Array";
            }

            return $"JSON Array [{this.Value.Count} items]";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            Type q = typeof(Q);
            JArray array = this.Value;
            if (q == typeof(JTokenGoo))
            {
                JToken token = array;
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
                        JArray array = goo.Value as JArray;
                        if (array != null)
                        {
                            this.Value = array;
                            return true;
                        }
                    }
                }
            }
            return base.CastFrom(source);
        }
    }
}
