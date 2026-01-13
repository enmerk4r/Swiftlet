using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Swiftlet.Components
{
    public class DeconstructRequestComponent : GH_Component
    {
        public DeconstructRequestComponent()
            : base("Deconstruct Request", "DeReq",
                "Extracts data from an HTTP request context.",
                NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ListenerRequestParam(), "Request", "R", "The HTTP listener request from Server Input", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ListenerRequestParam(), "Request", "R", "Pass-through request for Server Response", GH_ParamAccess.item);
            pManager.AddTextParameter("Method", "M", "HTTP method (GET, POST, etc.)", GH_ParamAccess.item);
            pManager.AddTextParameter("Route", "Rt", "Request path", GH_ParamAccess.item);
            pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Request headers", GH_ParamAccess.list);
            pManager.AddParameter(new QueryParamParam(), "Query", "Q", "Query string parameters", GH_ParamAccess.list);
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request body", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ListenerRequestGoo requestGoo = null;

            if (!DA.GetData(0, ref requestGoo))
            {
                return;
            }

            if (requestGoo == null || requestGoo.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid request provided");
                return;
            }

            HttpListenerContext context = requestGoo.Value;
            HttpListenerRequest request = context.Request;

            // Pass through the request unchanged
            DA.SetData(0, requestGoo);

            // Method
            DA.SetData(1, request.HttpMethod);

            // Route (path)
            DA.SetData(2, request.Url.AbsolutePath);

            // Headers
            List<HttpHeaderGoo> headers = new List<HttpHeaderGoo>();
            foreach (string key in request.Headers.AllKeys)
            {
                string value = request.Headers.GetValues(key)?.FirstOrDefault();
                headers.Add(new HttpHeaderGoo(key, value));
            }
            DA.SetDataList(3, headers);

            // Query parameters
            List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
            foreach (string key in request.QueryString.AllKeys)
            {
                if (key != null)
                {
                    string value = request.QueryString.GetValues(key)?.FirstOrDefault();
                    queryParams.Add(new QueryParamGoo(key, value));
                }
            }
            DA.SetDataList(4, queryParams);

            // Body - read bytes and create RequestBody with content type
            byte[] bodyBytes = new byte[0];
            string contentType = request.ContentType ?? "application/octet-stream";

            if (request.HasEntityBody)
            {
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        request.InputStream.CopyTo(memoryStream);
                        bodyBytes = memoryStream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to read request body: {ex.Message}");
                }
            }

            DA.SetData(5, new RequestBodyGoo(new RequestBodyByteArray(contentType, bodyBytes)));
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("B2C3D4E5-F6A7-8901-BCDE-F12345678901");
    }
}
