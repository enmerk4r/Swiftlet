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
            if (this.Value is JObject) return "JSON Object";
            else if (this.Value is JArray) return "JSON Array";
            else if (this.Value is JValue) return "JSON Value";
            else return "JSON Token";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            Type q = typeof(Q);
            JToken token = this.Value;
            if (q == typeof(JObjectGoo))
            {
                JObject obj = token as JObject;
                if (obj != null)
                {
                    object temp = new JObjectGoo(obj);
                    target = (Q)temp;
                    return true;
                }
            }
            else if (q == typeof(JArrayGoo))
            {
                JArray array = token as JArray;
                if (array != null)
                {
                    object temp = new JArrayGoo(array);
                    target = (Q)temp;
                    return true;
                }
            }
            else if (q == typeof(JValueGoo))
            {
                JValue value = token as JValue;
                if (value != null)
                {
                    object temp = new JValueGoo(value);
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
                if (source.GetType() == typeof(JArrayGoo))
                {
                    JArrayGoo goo = source as JArrayGoo;
                    if (goo != null)
                    {
                        JArray value = goo.Value as JArray;
                        if (value != null)
                        {
                            this.Value = value;
                            return true;
                        }
                    }
                }
                else if (source.GetType() == typeof(JObjectGoo))
                {
                    JObjectGoo goo = source as JObjectGoo;
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
                else if (source.GetType() == typeof(JValueGoo))
                {
                    JValueGoo goo = source as JValueGoo;
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
