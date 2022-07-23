using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using H.Socket.IO;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateSocketConnectionComponent : GH_Component
    {
        private SocketIoClient _client;
        private List<string> _logs;

        private string _connectionUrl = string.Empty;
        private string _oldUrl = string.Empty;

        /// <summary>
        /// Initializes a new instance of the GetRequestComponent class.
        /// </summary>
        public CreateSocketConnectionComponent()
          : base("Connect to Socket", "WSC",
              "Connect to a web socket",
              NamingUtility.CATEGORY, NamingUtility.SOCKETS)
        {
            this._logs = new List<string>();
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "URL for the web resource you're trying to reach", GH_ParamAccess.item);
            pManager.AddParameter(new QueryParamParam(), "Params", "P", "Query Params", GH_ParamAccess.list);
            pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Http Headers", GH_ParamAccess.list);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "L", "Socket connection log", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override async void SolveInstance(IGH_DataAccess DA)
        {
            string url = string.Empty;
            List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
            List<HttpHeaderGoo> httpHeaders = new List<HttpHeaderGoo>();

            DA.GetData(0, ref url);
            DA.GetDataList(1, queryParams);
            DA.GetDataList(2, httpHeaders);

            string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(o => o.Value).ToList());

            if (!string.IsNullOrEmpty(fullUrl))
            {

                this._connectionUrl = fullUrl;
                DA.SetDataList(0, this._logs);
                
            }
            else
            {
                throw new Exception("Invalid Url");
            }


        }

        protected override async void AfterSolveInstance()
        {
            base.AfterSolveInstance();
            if (this._connectionUrl != this._oldUrl)
            {
                if (!string.IsNullOrEmpty(this._connectionUrl))
                {
                    await this.ConnectToSocket(this._connectionUrl);
                    this._oldUrl = _connectionUrl;
                }
            }
        }

        private async Task ConnectToSocket(string url)
        {
            this._client = new SocketIoClient();

            this._client.Connected += (sender, args) =>
            {
                Log($"Connected: {args.Namespace}");
            };
            this._client.Disconnected += (sender, args) =>
            {
                Log($"Disconnected. Reason: {args.Reason}, Status: {args.Status:G}");
            };
            this._client.EventReceived += (sender, args) => Log($"EventReceived: Namespace: {args.Namespace}, Value: {args.Value}, IsHandled: {args.IsHandled}");
            this._client.HandledEventReceived += (sender, args) => Log($"HandledEventReceived: Namespace: {args.Namespace}, Value: {args.Value}");
            this._client.UnhandledEventReceived += (sender, args) => Log($"UnhandledEventReceived: Namespace: {args.Namespace}, Value: {args.Value}");
            this._client.ErrorReceived += (sender, args) => Log($"ErrorReceived: Namespace: {args.Namespace}, Value: {args.Value}");
            this._client.ExceptionOccurred += (sender, args) => Log($"ExceptionOccurred: {args.Value}");

            try
            {
                await this._client.ConnectAsync(new Uri(url));
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        private void Reset()
        {
            this._client = new SocketIoClient();
            this._logs = new List<string>();
        } 

        private void Log(string entry)
        {
            lock (this._logs)
            {
                this._logs.Add(entry);
                Grasshopper.Instances.ActiveCanvas.BeginInvoke(new Action(() => {
                    this.ExpireSolution(true);
                }));
                
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
                return Properties.Resources.Icons_get_request_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("db2c54b0-4858-4b26-8262-7bbd5e292f82"); }
        }
    }
}