using Swiftlet.DataModels.Interfaces;
using Swiftlet.Util;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Swiftlet.DataModels.Implementations
{
    public class MultipartField
    {
        public string Name { get; private set; }
        public string FileName { get; private set; }
        public string ContentType { get; private set; }
        public byte[] Bytes { get; private set; }
        public bool IsText { get; private set; }
        public Encoding Encoding { get; private set; }
        public string Text { get; private set; }

        public MultipartField(string name, byte[] bytes, string fileName = null, string contentType = null)
        {
            this.Name = name ?? string.Empty;
            this.FileName = fileName;
            this.ContentType = string.IsNullOrWhiteSpace(contentType)
                ? ContentTypeUtility.ApplicationOctetStream
                : contentType;
            this.Bytes = bytes?.ToArray() ?? new byte[0];
            this.IsText = false;
            this.Encoding = null;
            this.Text = null;
        }

        public MultipartField(string name, string text, string contentType = null, Encoding encoding = null)
        {
            this.Name = name ?? string.Empty;
            this.FileName = null;
            this.ContentType = string.IsNullOrWhiteSpace(contentType)
                ? ContentTypeUtility.TextPlain
                : contentType;
            this.Encoding = encoding ?? Encoding.UTF8;
            this.Text = text ?? string.Empty;
            this.Bytes = this.Encoding.GetBytes(this.Text);
            this.IsText = true;
        }

        public MultipartField(string name, IRequestBody body)
        {
            this.Name = name ?? string.Empty;
            this.FileName = null;
            this.ContentType = string.IsNullOrWhiteSpace(body?.ContentType)
                ? ContentTypeUtility.ApplicationOctetStream
                : body.ContentType;
            this.Bytes = body?.ToByteArray() ?? new byte[0];
            this.IsText = false;
            this.Encoding = null;
            this.Text = null;
        }

        public MultipartField Duplicate()
        {
            if (this.IsText)
            {
                return new MultipartField(this.Name, this.Text, this.ContentType, this.Encoding);
            }

            return new MultipartField(this.Name, this.Bytes, this.FileName, this.ContentType);
        }

        public HttpContent ToHttpContent()
        {
            if (this.IsText)
            {
                var content = new StringContent(this.Text ?? string.Empty, this.Encoding ?? Encoding.UTF8, this.ContentType);
                content.Headers.ContentType = new MediaTypeHeaderValue(this.ContentType);
                return content;
            }

            var byteContent = new ByteArrayContent(this.Bytes ?? new byte[0]);
            if (!string.IsNullOrWhiteSpace(this.ContentType))
            {
                byteContent.Headers.ContentType = new MediaTypeHeaderValue(this.ContentType);
            }
            return byteContent;
        }
    }
}
