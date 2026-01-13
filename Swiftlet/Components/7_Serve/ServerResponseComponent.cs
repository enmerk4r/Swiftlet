using Grasshopper.Kernel;
using Swiftlet.DataModels.Interfaces;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Net;

namespace Swiftlet.Components
{
    public class ServerResponseComponent : GH_Component
    {
        public ServerResponseComponent()
            : base("Server Response", "SR",
                "Sends an HTTP response to a pending request and closes the connection.",
                NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ListenerRequestParam(), "Request", "R", "The HTTP listener request from Server Input", GH_ParamAccess.item);
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Response body content", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Status", "S", "HTTP status code (default: 200)", GH_ParamAccess.item, 200);
            pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Response headers", GH_ParamAccess.list);

            pManager[1].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Success", "OK", "True if the response was sent successfully", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ListenerRequestGoo requestGoo = null;
            RequestBodyGoo bodyGoo = null;
            int statusCode = 200;
            List<HttpHeaderGoo> headerGoos = new List<HttpHeaderGoo>();

            if (!DA.GetData(0, ref requestGoo))
            {
                DA.SetData(0, false);
                return;
            }

            DA.GetData(1, ref bodyGoo);
            DA.GetData(2, ref statusCode);
            DA.GetDataList(3, headerGoos);

            if (requestGoo == null || requestGoo.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid request provided");
                DA.SetData(0, false);
                return;
            }

            HttpListenerContext context = requestGoo.Value;

            try
            {
                // Check if we can still write to the response
                if (!context.Response.OutputStream.CanWrite)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Response has already been sent or connection closed");
                    DA.SetData(0, false);
                    return;
                }

                // Set status code
                context.Response.StatusCode = statusCode;

                // Set headers
                foreach (var headerGoo in headerGoos)
                {
                    if (headerGoo?.Value != null)
                    {
                        context.Response.Headers[headerGoo.Value.Key] = headerGoo.Value.Value;
                    }
                }

                // Write body
                byte[] bodyBytes = bodyGoo?.Value?.ToByteArray() ?? new byte[0];

                // Set content type if body has one and not already set
                if (bodyGoo?.Value != null && !string.IsNullOrEmpty(bodyGoo.Value.ContentType))
                {
                    if (string.IsNullOrEmpty(context.Response.ContentType))
                    {
                        context.Response.ContentType = bodyGoo.Value.ContentType;
                    }
                }

                context.Response.ContentLength64 = bodyBytes.Length;
                context.Response.OutputStream.Write(bodyBytes, 0, bodyBytes.Length);
                context.Response.OutputStream.Close();

                DA.SetData(0, true);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to send response: {ex.Message}");
                DA.SetData(0, false);
            }
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("C3D4E5F6-A7B8-9012-CDEF-123456789012");
    }
}
