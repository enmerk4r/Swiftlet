using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;
using Swiftlet.DataModels.Interfaces;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class ListenerContextGoo : GH_Goo<HttpListenerContext>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "Http Listener Context";

        public override string TypeDescription => "Contains Http Request and Response information";

        public ListenerContextGoo()
        {
            this.Value = null;
        }

        public ListenerContextGoo(HttpListenerContext context)
        {
            this.Value = context;
        }

        public override IGH_Goo Duplicate()
        {
            return new ListenerContextGoo(this.Value);
        }

        public override string ToString()
        {
            return $"LISTENER CONTEXT";
        }
    }
}
