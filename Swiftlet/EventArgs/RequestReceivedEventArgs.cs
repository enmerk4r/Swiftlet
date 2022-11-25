using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet
{
    public class RequestReceivedEventArgs : EventArgs
    {
        public HttpListenerContext Context { get; set; }
        public RequestReceivedEventArgs(HttpListenerContext context)
        {
            this.Context = context;
        }
    }
}
