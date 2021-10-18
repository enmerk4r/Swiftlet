using Swiftlet.DataModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Implementations
{
    public class HttpResponseDTO : IHttpResponseDTO
    {
        public string Version { get; private set; }

        public int StatusCode { get; private set; }

        public string ReasonPhrase { get; private set; }

        public List<HttpHeader> Headers { get; private set; }

        public bool IsSuccessStatusCode { get; private set; }

        public string Content { get; private set; }

        public HttpResponseDTO()
        {
        }

        public HttpResponseDTO(HttpResponseMessage response)
        {
            this.Version = response.Version.ToString();
            this.StatusCode = (int)response.StatusCode;
            this.ReasonPhrase = response.ReasonPhrase;
            this.Headers = new List<HttpHeader>();
            foreach(var header in response.Headers)
            {
                string key = header.Key;
                string value = string.Empty;

                foreach(string h in header.Value)
                {
                    value += h + ",";
                }

                value = value.Substring(0, value.Length - 1);
                this.Headers.Add(new HttpHeader(key, value));
            }

            this.IsSuccessStatusCode = response.IsSuccessStatusCode;
            this.Content = response.Content.ReadAsStringAsync().Result;
        }

        public HttpResponseDTO(
            string version,
            int status,
            string reasonPhrase,
            List<HttpHeader> headers,
            bool isSuccess,
            string content
            )
        {
            this.Version = version;
            this.StatusCode = status;
            this.ReasonPhrase = reasonPhrase;
            this.Headers = new List<HttpHeader>();
            foreach(var h in headers)
            {
                this.Headers.Add(new HttpHeader(h.Key, h.Value));
            }
            this.IsSuccessStatusCode = isSuccess;
            this.Content = content;

        }



        public IHttpResponseDTO Duplicate()
        {
            return new HttpResponseDTO(
                this.Version,
                this.StatusCode,
                this.ReasonPhrase,
                this.Headers,
                this.IsSuccessStatusCode,
                this.Content);
        }
    }
}
