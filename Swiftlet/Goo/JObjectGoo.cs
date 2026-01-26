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
            if (this.Value == null)
            {
                return "JSON Object";
            }

            return $"JSON Object [{this.Value.Count} keys]";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            Type q = typeof(Q);
            JObject obj = this.Value;
            if (q == typeof(JTokenGoo))
            {
                JToken token = obj;
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
                        JObject value = goo.Value as JObject;
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
