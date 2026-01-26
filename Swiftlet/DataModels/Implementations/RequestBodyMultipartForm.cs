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

        public List<MultipartField> Fields
        {
            get
            {
                return this.Value as List<MultipartField>;
            }
            private set
            {
                this.Value = value;
            }
        }

        public RequestBodyMultipartForm()
        {
            this.Fields = new List<MultipartField>();
        }

        public RequestBodyMultipartForm(List<MultipartField> fields)
        {
            this.Fields = new List<MultipartField>();
            foreach (MultipartField field in fields ?? new List<MultipartField>())
            {
                this.Fields.Add(field?.Duplicate() ?? new MultipartField(string.Empty, new byte[0]));
            }
        }

        public RequestBodyMultipartForm(List<KeyValuePair<string, IRequestBody>> fields)
        {
            this.Fields = new List<MultipartField>();
            foreach (var field in fields ?? new List<KeyValuePair<string, IRequestBody>>())
            {
                this.Fields.Add(new MultipartField(field.Key, field.Value));
            }
        }

        public RequestBodyMultipartForm(List<IRequestBody> fields)
        {
            this.Fields = new List<MultipartField>();
            foreach (IRequestBody field in fields ?? new List<IRequestBody>())
            {
                this.Fields.Add(new MultipartField(string.Empty, field));
            }
        }

        public RequestBodyMultipartForm(List<string> keys, List<IRequestBody> fields)
        {
            if (keys.Count != fields.Count) throw new Exception("The number of keys must match the number of fields");
            this.Fields = new List<MultipartField>();

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                IRequestBody field = fields[i];

                this.Fields.Add(new MultipartField(key, field));
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
            foreach (MultipartField field in this.Fields ?? new List<MultipartField>())
            {
                HttpContent content = field?.ToHttpContent() ?? new ByteArrayContent(new byte[0]);
                string name = field?.Name;
                string fileName = field?.FileName;

                if (name == null)
                {
                    form.Add(content);
                }
                else if (!string.IsNullOrEmpty(fileName))
                {
                    form.Add(content, name, fileName);
                }
                else
                {
                    form.Add(content, name);
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
