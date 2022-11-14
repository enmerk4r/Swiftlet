using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
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
    public class HttpRequestComponent : GH_Component
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
            pManager.AddTextParameter("URL", "U", "URL for the web resource you're trying to reach", GH_ParamAccess.tree);
            pManager.AddTextParameter("Method", "M", "HTTP method: \"GET\", \"POST\", \"PUT\", \"DELETE\", \"PATCH\", \"HEAD\", \"CONNECT\", \"OPTIONS\", \"TRACE\"", GH_ParamAccess.tree);
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "POST Body", GH_ParamAccess.tree);
            pManager.AddParameter(new QueryParamParam(), "Params", "P", "Query Params", GH_ParamAccess.tree);
            pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Http Headers", GH_ParamAccess.tree);

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
            GH_Structure<GH_String> urls = null;
            GH_Structure<GH_String> methods = null;
            
            GH_Structure<RequestBodyGoo> bodyGoos = null;
            GH_Structure<QueryParamGoo> queryParams = null;
            GH_Structure<HttpHeaderGoo> httpHeaders = null;

            DA.GetDataTree(0, out urls);
            DA.GetDataTree(1, out methods);
            DA.GetDataTree(2, out bodyGoos);
            DA.GetDataTree(3, out queryParams);
            DA.GetDataTree(4, out httpHeaders);

            GH_SimplificationMode sMode = GH_SimplificationMode.CollapseLeadingOverlaps;
            urls.Simplify(sMode);
            methods.Simplify(sMode);
            bodyGoos.Simplify(sMode);
            queryParams.Simplify(sMode);
            httpHeaders.Simplify(sMode);


            if (urls.PathCount != methods.PathCount)
            {
                throw new Exception("The number of provided URLs does not match the number of provided HTTP methods. Please, check your data tree structure");
            }

            List<GH_Path> paths = urls.Paths.ToList();

            List<HttpRequestPackage> requests = new List<HttpRequestPackage>();
            List<Task<HttpResponseDTO>> tasks = new List<Task<HttpResponseDTO>>();

            foreach(GH_Path path in paths) 
            {
                string url = ((List<GH_String>)urls.get_Branch(path)).First().Value;
                string method = ((List<GH_String>)methods.get_Branch(path)).First().Value;

                IRequestBody body = null;
                List<QueryParam> qParams = new List<QueryParam>();
                List<HttpHeader> headers = new List<HttpHeader>();

                try
                {
                    body = ((List<RequestBodyGoo>)bodyGoos.get_Branch(path)).First().Value;
                }
                catch { }

                try
                {
                    qParams = ((List<QueryParamGoo>)queryParams.get_Branch(path)).Select(p => p.Value).ToList();
                }
                catch { }

                try
                {
                    headers = ((List<HttpHeaderGoo>)httpHeaders.get_Branch(path)).Select(p => p.Value).ToList();
                }
                catch { }

                HttpRequestPackage package = new HttpRequestPackage(url, method, body, qParams, headers, path);
                requests.Add(package);
                tasks.Add(package.GetResponse());
            }

            List<HttpResponseDTO> responses = tasks.Select(t => t.Result).ToList();



            


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
                return Properties.Resources.Icons_get_request_24x24;
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

    class HttpRequestPackage 
    { 
        public string Url { get; }
        public string Method { get; }
        public IRequestBody Body { get; }
        public List<QueryParam> QueryParams { get; }
        public List<HttpHeader> HttpHeaders { get; }
        public GH_Path Path { get; }
        public HttpRequestPackage(string url, string method, IRequestBody body, List<QueryParam> queryParams, List<HttpHeader> headers, GH_Path path)
        {
            this.Url = url;
            this.Method = method.ToUpper();
            this.Body = body;
            this.QueryParams = queryParams.Select(o => o).ToList();
            this.HttpHeaders = headers.Select(o => o).ToList();
            this.Path = path;
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

        public async Task<HttpResponseDTO> GetResponse()
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
                        response = await client.PostAsync(fullUrl, content);
                    }
                    else if (this.Method == "PUT") 
                    {
                        HttpContent content = this.Body.ToHttpContent();
                        response = await client.PutAsync(fullUrl, content);
                    }
                    else if (this.Method == "PATCH")
                    {
                        HttpContent content = this.Body.ToHttpContent();
                        response = await this.PatchAsync(client, fullUrl, content);
                    }
                    else
                    {
                        HttpRequestMessage msg = this.GetRequestMessage();
                        response = await client.SendAsync(msg);
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