using Swiftlet.DataModels.Interfaces;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Implementations
{
    public class RequestBodyMultipartForm : IRequestBody
    {
        public string ContentType => this.CompileForm().Headers.ContentType.ToString();

        public object Value { get; private set; }

        public List<KeyValuePair<string, IRequestBody>> Fields
        {
            get
            {
                return this.Value as List<KeyValuePair<string, IRequestBody>>;
            }
            private set
            {
                this.Value = value;
            }
        }

        public RequestBodyMultipartForm()
        {
        }

        public RequestBodyMultipartForm(List<KeyValuePair<string, IRequestBody>> fields)
        {
            this.Fields = new List<KeyValuePair<string, IRequestBody>>();
            foreach (var field in fields)
            {
                this.Fields.Add(new KeyValuePair<string, IRequestBody>(field.Key, field.Value));
            }
        }

        public RequestBodyMultipartForm(List<IRequestBody> fields)
        {
            this.Fields = new List<KeyValuePair<string, IRequestBody>>();
            foreach (IRequestBody field in fields)
            {
                this.Fields.Add(new KeyValuePair<string, IRequestBody>(null, field));
            }
        }

        public RequestBodyMultipartForm(List<string> keys, List<IRequestBody> fields)
        {
            if (keys.Count != fields.Count) throw new Exception("The number of keys must match the number of fields");
            this.Fields = new List<KeyValuePair<string, IRequestBody>>();

            for (int i=0; i < keys.Count; i++)
            {
                string key = keys[i];
                IRequestBody field = fields[i];

                this.Fields.Add(new KeyValuePair<string, IRequestBody>(key, field));
            }
        }

        public IRequestBody Duplicate()
        {
            return new RequestBodyMultipartForm(this.Fields);
        }

        public HttpContent ToHttpContent()
        {
            var form = CompileForm();
            return form;
        }

        public MultipartFormDataContent CompileForm()
        {
            var form = new MultipartFormDataContent();
            foreach (var field in this.Fields)
            {
                if (!string.IsNullOrEmpty(field.Key))
                {
                    form.Add(field.Value.ToHttpContent(), field.Key);
                }
                else
                {
                    form.Add(field.Value.ToHttpContent());
                }
            }
            return form;
        }

        public byte[] ToByteArray()
        {
            var form = CompileForm();
            using (var ms = new MemoryStream())
            {
                form.CopyToAsync(ms).GetAwaiter().GetResult();
                return ms.ToArray();
            }
        }
    }
}
