using Grasshopper.Kernel.Types;
using Swiftlet.DataModels.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Goo
{
    public class HttpWebResponseGoo : GH_Goo<HttpResponseDTO>
    {
        public override bool IsValid => this.Value != null;

        public override string TypeName => "Http Web Response";

        public override string TypeDescription => "The result of your Http Web Request";

        public HttpWebResponseGoo()
        {
            this.Value = null;
        }

        public HttpWebResponseGoo(HttpResponseDTO response)
        {
            this.Value = (HttpResponseDTO)response.Duplicate();
        }

        public HttpWebResponseGoo(HttpResponseMessage response)
        {
            this.Value = new HttpResponseDTO(response);
        }

        public override IGH_Goo Duplicate()
        {
            return new HttpWebResponseGoo(this.Value);
        }

        public override string ToString()
        {
            return $"Http Response [{this.Value.StatusCode}]";
        }
    }
}
