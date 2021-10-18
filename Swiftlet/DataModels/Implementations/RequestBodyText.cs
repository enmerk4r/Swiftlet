using Swiftlet.DataModels.Enums;
using Swiftlet.DataModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Implementations
{
    public class RequestBodyText : IRequestBody
    {
        public ContentType ContentType { get; private set; }

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
            this.ContentType = ContentType.Text;
        }

        public RequestBodyText(ContentType type, string text)
        {
            this.ContentType = type;
            this.Text = text;
        }

        public IRequestBody Duplicate()
        {
            return new RequestBodyText(this.ContentType, this.Text);
        }
    }
}
