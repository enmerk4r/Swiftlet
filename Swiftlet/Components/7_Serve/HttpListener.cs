using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;
using Swiftlet.DataModels.Interfaces;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;

namespace Swiftlet.Components
{
    public class HttpListenerComponent : GH_Component
    {

        private HttpListener _listener;
        public HttpListener Listener
        {
            get
            {
                if (_listener == null)
                {
                    this._listener = new HttpListener();
                }
                return this._listener;
            }
        }

        public Task ListenerTask { get; set; }

        private HttpListenerContext _context;

        private string _requestBody;
        private IRequestBody _responseBody;

        private List<HttpHeaderGoo> _headerGoos;
        private List<QueryParamGoo> _queryParamGoos;

        /// <summary>
        /// Initializes a new instance of the HttpListener class.
        /// </summary>
        public HttpListenerComponent()
          : base("HTTP Listener", "L",
              "A simple HTTP listener component that can receive HTTP requests",
              NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
            
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("Scheme", "S", "http or https", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Port", "P", "Port number to listen at between 0 and 65353", GH_ParamAccess.item);
            pManager.AddTextParameter("Route", "R", "An optional path", GH_ParamAccess.item);
            pManager.AddParameter(new RequestBodyParam(), "Response Body", "B", "Pre-canned response body that will be returned to the sender", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Method", "M", "Request Method", GH_ParamAccess.item);
            pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Http Request Headers", GH_ParamAccess.list);
            pManager.AddParameter(new QueryParamParam(), "Query Params", "Q", "Components of the request query string", GH_ParamAccess.list);
            pManager.AddTextParameter("Content", "C", "Request Content", GH_ParamAccess.item);
            //pManager.AddParameter(new ListenerContextParam(), "Context", "CT", "Listener Context", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string scheme = "http";
            int port = 80;
            string route = string.Empty;
            RequestBodyGoo bodyGoo = null;

            //DA.GetData(0, ref scheme);
            DA.GetData(0, ref port);
            DA.GetData(1, ref route);
            DA.GetData(2, ref bodyGoo);

            if (bodyGoo != null)
            {
                _responseBody = bodyGoo.Value;
            }

            //if (string.IsNullOrEmpty(scheme)) return;

            //scheme = scheme.ToLower();

            this.Listener.Close();
            this._listener = null;

            _headerGoos = new List<HttpHeaderGoo>();
            _queryParamGoos = new List<QueryParamGoo>();

            if (scheme != "http" && scheme != "https")
            {
                throw new Exception("Scheme must be either \"http\" or \"https\"");
            }

            if (port < 0 || port > 65353)
            {
                throw new Exception("Port number must be between 0 and 65353");
            }

            if (!string.IsNullOrEmpty(route) && !route.EndsWith("/")) route += "/";

            string uri = $"{scheme}://localhost:{port}/{route}";
            this.Message = uri;

            this.Listener.Prefixes.Clear();
            this.Listener.Prefixes.Add(uri);

            this.HttpRequestReceived -= HttpListenerComponent_RequestReceived;
            this.HttpRequestReceived += HttpListenerComponent_RequestReceived;

            
            DA.SetData(0, this._context?.Request.HttpMethod);

            List<HttpHeaderGoo> headerGoos = new List<HttpHeaderGoo>();
            List<QueryParamGoo> queryParamGoos = new List<QueryParamGoo>();

            
            DA.SetDataList(1, _headerGoos);
            DA.SetDataList(2, _queryParamGoos);
            DA.SetData(3, _requestBody);
            //DA.SetData(4, new ListenerContextGoo(this._context));

        }

        protected override void AfterSolveInstance()
        {
            base.AfterSolveInstance();
            this.ListenerTask = Task.Run(() =>
            {
                this.ListenForRequests();
            });
        }

        private void HttpListenerComponent_RequestReceived(object sender, EventArgs e)
        {
            RequestReceivedEventArgs args = e as RequestReceivedEventArgs;
            this._context = args.Context;

            Grasshopper.Instances.ActiveCanvas.BeginInvoke(new Action(() =>
            {
                this.ExpireSolution(true);
            }));
        }

        public void ListenForRequests()
        {
            try
            {
                this.Listener.Start();
                var context = this.Listener.GetContext();
                this._requestBody = null;

                if (_context != null)
                {
                    if (context.Request.HasEntityBody)
                    {
                        _requestBody = new StreamReader(context.Request.InputStream).ReadToEnd();
                    }


                    // Get Headers
                    foreach (var key in this._context.Request.Headers.AllKeys)
                    {
                        HttpHeaderGoo goo = new HttpHeaderGoo(key, this._context.Request.Headers.GetValues(key).FirstOrDefault());
                        _headerGoos.Add(goo);
                    }

                    // Get Query Params
                    foreach (var key in this._context.Request.QueryString.AllKeys)
                    {
                        QueryParamGoo goo = new QueryParamGoo(key, this._context.Request.QueryString.GetValues(key).FirstOrDefault());
                        _queryParamGoos.Add(goo);
                    }


                    // Send response
                    byte[] response = new byte[0];
                    if (this._responseBody != null)
                    {
                        response = this._responseBody.ToByteArray();
                    }
                    context.Response.OutputStream.Write(response, 0, response.Length);
                    context.Response.OutputStream.Close();
                }
                this.OnHttpRequestReceived(new RequestReceivedEventArgs(context));
                return;
            }
            catch
            {

            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            this.CleanUp();
            base.RemovedFromDocument(document);
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Close)
            {
                this.CleanUp();
            }
            base.DocumentContextChanged(document, context);
        }

        public void CleanUp()
        {
            try
            {
                this.Listener.Close();
                this._listener = null;

                this.ListenerTask?.Dispose();
            }
            catch
            {

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
                return Properties.Resources.Icons_http_listener_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("EB521F46-C8EE-49B3-AD30-0EAFB8B2B75E"); }
        }

        public EventHandler HttpRequestReceived;
        public void OnHttpRequestReceived(RequestReceivedEventArgs e)
        {
            EventHandler handler = HttpRequestReceived;
            handler?.Invoke(this, e);
        }
    }
}