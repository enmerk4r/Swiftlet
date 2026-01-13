using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System.Net.Http;
using Swiftlet.DataModels.Interfaces;
using System.Threading.Tasks;

namespace Swiftlet.Components
{
    public class PostRequestComponent : BaseRequestComponent
    {
        /// <summary>
        /// Initializes a new instance of the PostRequestComponent class.
        /// </summary>
        public PostRequestComponent()
          : base("POST Request", "POST",
              "Send a POST request",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "URL for the web resource you're trying to reach", GH_ParamAccess.item);
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "POST Body", GH_ParamAccess.item);
            pManager.AddParameter(new QueryParamParam(), "Params", "P", "Query Params", GH_ParamAccess.list);
            pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Http Headers", GH_ParamAccess.list);
            

            pManager[2].Optional = true;
            pManager[3].Optional = true;
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

        public HttpResponseDTO SendRequest(string url, RequestBodyGoo bodyGoo, List<QueryParamGoo> queryParams, List<HttpHeaderGoo> httpHeaders)
        {
            ValidateUrl(url);
            string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(o => o.Value).ToList());

            var body = bodyGoo.Value;

            if (!string.IsNullOrEmpty(fullUrl))
            {
                // Use HttpRequestMessage with shared client (add headers to request, not client)
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, fullUrl);

                // Add headers to the request message
                foreach (HttpHeaderGoo header in httpHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Value.Key, header.Value.Value);
                }

                // Add body content
                request.Content = body.ToHttpContent();

                var result = HttpClientFactory.SharedClient.SendAsync(request).Result;
                HttpResponseDTO dto = new HttpResponseDTO(result);

                return dto;
            }
            else
            {
                throw new Exception("Invalid Url");
            }
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
                RequestBodyGoo bodyGoo = null;
                List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
                List<HttpHeaderGoo> httpHeaders = new List<HttpHeaderGoo>();

                DA.GetData(0, ref url);
                DA.GetData(1, ref bodyGoo);
                DA.GetDataList(2, queryParams);
                DA.GetDataList(3, httpHeaders);

                ValidateUrl(url);

                this.TaskList.Add(Task.Run(
                    () => { return new HttpRequestSolveResults() { Value = this.SendRequest(url, bodyGoo, queryParams, httpHeaders) }; },
                    CancelToken
                    ));
                return;
            }

            if (!GetSolveResults(DA, out HttpRequestSolveResults result))
            {
                string url = string.Empty;
                RequestBodyGoo bodyGoo = null;
                List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
                List<HttpHeaderGoo> httpHeaders = new List<HttpHeaderGoo>();

                DA.GetData(0, ref url);
                DA.GetData(1, ref bodyGoo);
                DA.GetDataList(2, queryParams);
                DA.GetDataList(3, httpHeaders);

                ValidateUrl(url);

                result = new HttpRequestSolveResults() { Value = this.SendRequest(url, bodyGoo, queryParams, httpHeaders) };
            }

            if (result != null)
            {
                DA.SetData(0, result.Value.StatusCode);
                DA.SetData(1, result.Value.Content);
                DA.SetData(2, new HttpWebResponseGoo(result.Value));
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
                return Properties.Resources.Icons_post_request_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("18931e0e-bb79-4863-8e0f-4a0f13a137a6"); }
        }
    }
}