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
    public class PatchRequestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PostRequestComponent class.
        /// </summary>
        public PatchRequestComponent()
          : base("PATCH Request", "PATCH",
              "Send a PATCH request",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "URL for the web resource you're trying to reach", GH_ParamAccess.item);
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "PATCH Body", GH_ParamAccess.item);
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

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string url = string.Empty;
            RequestBodyGoo bodyGoo = null;
            List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
            List<HttpHeaderGoo> httpHeaders = new List<HttpHeaderGoo>();

            DA.GetData(0, ref url);
            DA.GetData(1, ref bodyGoo);
            DA.GetDataList(2, queryParams);
            DA.GetDataList(3, httpHeaders);

            if (string.IsNullOrEmpty(url)) throw new Exception("Invalid Url");
            if (!url.StartsWith("http")) throw new Exception("Please, make sure your URL starts with 'http' or 'https'");

            string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(o => o.Value).ToList());

            var body = bodyGoo.Value;

            if (!string.IsNullOrEmpty(fullUrl))
            {

                HttpClient client = new HttpClient();

                // Add headers
                foreach (HttpHeaderGoo header in httpHeaders)
                {
                    client.DefaultRequestHeaders.Add(header.Value.Key, header.Value.Value);
                }

                HttpContent content = null;
                if (body is RequestBodyText)
                {
                    content = new StringContent(((RequestBodyText)body).Text, System.Text.Encoding.UTF8, body.ContentType);
                }
                var result = this.PatchAsync(client, fullUrl, content);

                HttpResponseDTO dto = new HttpResponseDTO(result);

                DA.SetData(0, dto.StatusCode);
                DA.SetData(1, dto.Content);
                DA.SetData(2, new HttpWebResponseGoo(dto));



            }
            else
            {
                throw new Exception("Invalid Url");
            }

        }

        public HttpResponseMessage PatchAsync(HttpClient client, string requestUri, HttpContent iContent)
        {
            var method = new HttpMethod("PATCH");

            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            HttpResponseMessage response = null;

            try
            {
                var task = client.SendAsync(request);
                response = task.Result;
            }
            catch (TaskCanceledException e)
            {
            }

            return response;
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
                return Properties.Resources.Icons_patch_request_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2ebe4cc3-334b-4e80-bd1d-59e66e70da46"); }
        }
    }
}