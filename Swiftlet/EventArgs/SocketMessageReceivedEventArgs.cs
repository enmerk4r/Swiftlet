using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet
{
    public class SocketMessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public SocketMessageReceivedEventArgs(string message)
        {
            this.Message = message;
        }
    }
}
