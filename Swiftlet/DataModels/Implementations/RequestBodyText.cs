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
    public class RequestBodyText : IRequestBody
    {
        public string ContentType { get; private set; }

        public object Value { get; private set; }

        public string Text
        {
            get
            {
                return this.Value as string;
            }
            private set
            {
                this.Value = value;
            }
        }

        public RequestBodyText()
        {
            this.ContentType = ContentTypeUtility.TextPlain;
        }

        public RequestBodyText(string type, string text)
        {
            this.ContentType = type;
            this.Text = text;
        }

        public IRequestBody Duplicate()
        {
            return new RequestBodyText(this.ContentType, this.Text);
        }

        public HttpContent ToHttpContent()
        {
            var content = new StringContent(this.Text, Encoding.UTF8, this.ContentType);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(this.ContentType);
            return content;
        }

        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(this.Text);
        }
    }
}
