using Swiftlet.DataModels.Interfaces;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Implementations
{
    public class RequestBodyByteArray : IRequestBody
    {
        public string ContentType { get; private set; }

        public object Value { get; private set; }

        public byte[] Content
        {
            get
            {
                return this.Value as byte[];
            }
            private set
            {
                this.Value = value;
            }
        }

        public RequestBodyByteArray()
        {
        }

        public RequestBodyByteArray(string type, byte[] content)
        {
            this.ContentType = type;
            this.Content = content;
        }

        public IRequestBody Duplicate()
        {
            return new RequestBodyByteArray(this.ContentType, this.Content);
        }

        public HttpContent ToHttpContent()
        {
            return new ByteArrayContent(this.Content);
        }
        public byte[] ToByteArray()
        {
            return this.Content;
        }

    }
}
