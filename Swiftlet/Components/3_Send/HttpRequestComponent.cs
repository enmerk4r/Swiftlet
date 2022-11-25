using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Documentation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.DataModels.Interfaces;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Swiftlet.Components
{
    public class HttpRequestComponent : GH_TaskCapableComponent<HttpRequestSolveResults>
    {
        /// <summary>
        /// Initializes a new instance of the GetRequestComponent class.
        /// </summary>
        public HttpRequestComponent()
          : base("HTTP Request", "REQ",
              "A multithreaded component that supports all HTTP methods",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }


        public override GH_Exposure Exposure => GH_Exposure.secondary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "URL for the web resource you're trying to reach", GH_ParamAccess.item);
            pManager.AddTextParameter("Method", "M", "HTTP method: \"GET\", \"POST\", \"PUT\", \"DELETE\", \"PATCH\", \"HEAD\", \"CONNECT\", \"OPTIONS\", \"TRACE\"", GH_ParamAccess.item);
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "POST Body", GH_ParamAccess.item);
            pManager.AddParameter(new QueryParamParam(), "Params", "P", "Query Params", GH_ParamAccess.list);
            pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Http Headers", GH_ParamAccess.list);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Status", "S", "Http Status Code", GH_ParamAccess.item);
            pManager.AddTextParameter("Content", "C", "Http response body", GH_ParamAccess.item);
            pManager.AddParameter(new HttpWebResponseParam(), "Response", "R", "Full Http response object (with metadata)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (InPreSolve)
            {
                string url = string.Empty;
                string method = string.Empty;
                RequestBodyGoo bodyGoo = null;

                List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
                List<HttpHeaderGoo> httpHeaders = new List<HttpHeaderGoo>();

                DA.GetData(0, ref url);
                DA.GetData(1, ref method);
                DA.GetData(2, ref bodyGoo);
                DA.GetDataList(3, queryParams);
                DA.GetDataList(4, httpHeaders);

                if (string.IsNullOrEmpty(url)) throw new Exception("Invalid Url");
                if (!url.StartsWith("http")) throw new Exception("Please, make sure your URL starts with 'http' or 'https'");

                string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(o => o.Value).ToList());

                HttpRequestPackage package = new HttpRequestPackage(fullUrl, method, bodyGoo?.Value, queryParams.Select(q => q.Value).ToList(), httpHeaders.Select(h => h.Value).ToList());
                this.TaskList.Add(Task.Run(
                    () => { return new HttpRequestSolveResults() { Value = package.GetResponse() }; }, 
                    CancelToken
                    ));
                return;
            }

            if (!GetSolveResults(DA, out HttpRequestSolveResults result))
            {
                string url = string.Empty;
                string method = string.Empty;
                RequestBodyGoo bodyGoo = null;

                List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
                List<HttpHeaderGoo> httpHeaders = new List<HttpHeaderGoo>();

                DA.GetData(0, ref url);
                DA.GetData(1, ref method);
                DA.GetData(2, ref bodyGoo);
                DA.GetDataList(3, queryParams);
                DA.GetDataList(4, httpHeaders);

                if (string.IsNullOrEmpty(url)) throw new Exception("Invalid Url");
                if (!url.StartsWith("http")) throw new Exception("Please, make sure your URL starts with 'http' or 'https'");

                string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(o => o.Value).ToList());

                HttpRequestPackage package = new HttpRequestPackage(fullUrl, method, bodyGoo?.Value, queryParams.Select(q => q.Value).ToList(), httpHeaders.Select(h => h.Value).ToList());
                result = new HttpRequestSolveResults() { Value = package.GetResponse() };
                return;
            }


            if (result != null)
            {

                DA.SetData(0, result.Value.StatusCode);
                DA.SetData(1, result.Value.Content);
                DA.SetData(2,  new HttpWebResponseGoo(result.Value));
            }


        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.Icons_http_request_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("cc8b1ecf-a2e7-495c-8d41-1e84011633bf"); }
        }
    }


    public class HttpRequestPackage 
    { 
        public string Url { get; }
        public string Method { get; }
        public IRequestBody Body { get; }
        public List<QueryParam> QueryParams { get; }
        public List<HttpHeader> HttpHeaders { get; }
        public HttpRequestPackage(string url, string method, IRequestBody body, List<QueryParam> queryParams, List<HttpHeader> headers)
        {
            this.Url = url;
            this.Method = method.ToUpper();
            this.Body = body;
            this.QueryParams = queryParams.Select(o => o).ToList();
            this.HttpHeaders = headers.Select(o => o).ToList();
        }

        private HttpMethod GetHttpMethod()
        {
            switch (this.Method)
            {
                case "GET":
                    return HttpMethod.Get;
                case "POST":
                    return HttpMethod.Post;
                case "PUT":
                    return HttpMethod.Put;
                case "DELETE": 
                    return HttpMethod.Delete;
                case "PATCH":
                    return new HttpMethod("PATCH");

                case "HEAD":
                    return HttpMethod.Head;
                case "TRACE":
                    return HttpMethod.Trace;
                case "CONNECT":
                    return new HttpMethod("CONNECT");
                case "OPTIONS":
                    return HttpMethod.Options;

                default:
                    return new HttpMethod(this.Method);
            }
        }

        public HttpRequestMessage GetRequestMessage()
        {
            HttpRequestMessage msg = new HttpRequestMessage(this.GetHttpMethod(), this.Url);
            return msg;
        }

        public HttpResponseDTO GetResponse()
        {
            try
            {
                if (string.IsNullOrEmpty(this.Url)) throw new Exception("Invalid Url");
                if (!this.Url.StartsWith("http")) throw new Exception("Please, make sure your URL starts with 'http' or 'https'");

                string fullUrl = UrlUtility.AddQueryParams(this.Url, this.QueryParams);

                if (!string.IsNullOrEmpty(fullUrl))
                {
                    HttpClient client = new HttpClient();

                    HttpResponseMessage response = null;

                    // Add headers
                    foreach (HttpHeader header in this.HttpHeaders)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }

                    if (this.Method == "POST")
                    {
                        HttpContent content = this.Body.ToHttpContent();
                        response = client.PostAsync(fullUrl, content).Result;
                    }
                    else if (this.Method == "PUT") 
                    {
                        HttpContent content = this.Body.ToHttpContent();
                        response = client.PutAsync(fullUrl, content).Result;
                    }
                    else if (this.Method == "PATCH")
                    {
                        HttpContent content = this.Body.ToHttpContent();
                        response = this.PatchAsync(client, fullUrl, content).Result;
                    }
                    else
                    {
                        HttpRequestMessage msg = this.GetRequestMessage();
                        response = client.SendAsync(msg).Result;
                    }

                    HttpResponseDTO dto = new HttpResponseDTO(response);

                    return dto;
                }
                else
                {
                    throw new Exception("Invalid Url");
                }
            }
            catch (Exception exc)
            {
                return new HttpResponseDTO(null, -1, exc.Message, new List<HttpHeader>(), false, null, new byte[0]);
            }
        }

        public Task<HttpResponseMessage> PatchAsync(HttpClient client, string requestUri, HttpContent iContent)
        {
            var method = new HttpMethod("PATCH");

            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            Task<HttpResponseMessage> response = null;

            try
            {
                var task = client.SendAsync(request);
                response = task;
            }
            catch (TaskCanceledException e)
            {
            }

            return response;
        }
    }
}