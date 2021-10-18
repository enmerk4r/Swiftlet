using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class HttpHeaderGoo : GH_Goo<HttpHeader>
    {
        public override bool IsValid => this.Value != null && !string.IsNullOrEmpty(this.Value.Key);

        public override string TypeName => "Http Header";

        public override string TypeDescription => "A header for an Http Web Request";

        public HttpHeaderGoo()
        {
            this.Value = new HttpHeader(null, null);
        }

        public HttpHeaderGoo(HttpHeader header)
        {
            this.Value = new HttpHeader(header.Key, header.Value);
        }

        public HttpHeaderGoo(string key, string value)
        {
            this.Value = new HttpHeader(key, value);
        }

        public override IGH_Goo Duplicate()
        {
            return new HttpHeaderGoo(this.Value.Key, this.Value.Value);
        }

        public override string ToString()
        {
            return $"HEADER [ {this.Value.Key} | {this.Value.Value} ]";
        }
    }
}
