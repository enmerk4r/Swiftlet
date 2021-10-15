using Swiftlet.DataModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Implementations
{
    public class HttpResponseDTO : IHttpResponseDTO
    {
        public HttpResponseDTO()
        {
        }

        public HttpResponseDTO(HttpWebResponse response, string content=null)
        {
            this.CharacterSet = response.CharacterSet;
            this.ContentEncoding = response.ContentEncoding;
            this.ContentLength = response.ContentLength;
            this.ContentType = response.ContentType;
            this.IsFromCache = response.IsFromCache;
            this.LastModified = response.LastModified;
            this.Method = response.Method;
            this.ResponseUri = response.ResponseUri.AbsoluteUri;
            this.ResponseServer = response.Server;
            this.StatusCode = (int)response.StatusCode;
            this.StatusDescription = response.StatusDescription;
            this.SupportsHeaders = response.SupportsHeaders;
            this.Content = content;
        }

        public HttpResponseDTO(
            string charSet,
            string contentEncoding,
            long contentLength,
            string contentType,
            bool isFromCache,
            DateTime lastModified,
            string method,
            string responseUri,
            string responseServer,
            int statusCode,
            string statusDescription,
            bool supportsHeaders,
            string content
            )
        {
            this.CharacterSet = charSet;
            this.ContentEncoding = contentEncoding;
            this.ContentLength = contentLength;
            this.ContentType = contentType;
            this.IsFromCache = isFromCache;
            this.LastModified = lastModified;
            this.Method = method;
            this.ResponseUri = responseUri;
            this.ResponseServer = responseServer;
            this.StatusCode = statusCode;
            this.StatusDescription = StatusDescription;
            this.SupportsHeaders = supportsHeaders;
            this.Content = content;
        }

        public string CharacterSet { get; private set; }

        public string ContentEncoding { get; private set; }

        public long ContentLength { get; private set; }

        public string ContentType { get; private set; }

        public bool IsFromCache { get; private set; }

        public DateTime LastModified { get; private set; }

        public string Method { get; private set; }

        public string ResponseUri { get; private set; }

        public string ResponseServer { get; private set; }

        public int StatusCode { get; private set; }

        public string StatusDescription { get; private set; }

        public bool SupportsHeaders { get; private set; }

        public string Content { get; private set; }


        public IHttpResponseDTO Duplicate()
        {
            return new HttpResponseDTO(
                this.CharacterSet,
                this.ContentEncoding,
                this.ContentLength,
                this.ContentType,
                this.IsFromCache,
                this.LastModified,
                this.Method,
                this.ResponseUri,
                this.ResponseServer,
                this.StatusCode,
                this.StatusDescription,
                this.SupportsHeaders,
                this.Content);
        }
    }
}
