using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components._7_Serve
{
    public class SocketListener : GH_Component
    {
        ClientWebSocket _client;
        ClientWebSocket Client
        {
            get
            {
                if (_client == null) 
                {
                    this._client = new ClientWebSocket();
                }
                return this._client;
            }
        }

        private Task _runningTask;

        private string _url;
        private List<QueryParam> _queryParams;
        private List<string> _onOpen;

        private string lastMessage;

        private bool _resetClient = true;

        /// <summary>
        /// Initializes a new instance of the SocketListener class.
        /// </summary>
        public SocketListener()
          : base("Socket Listener", "SOCKET",
              "A simple socket listener component",
              NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "URL of WebSocket resource", GH_ParamAccess.item);
            pManager.AddParameter(new QueryParamParam(), "Params", "P", "Query Params", GH_ParamAccess.list);
            pManager.AddTextParameter("On Open", "O", "Messages to be sent to the WebSocket server after opening the connection", GH_ParamAccess.list);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Messages", "M", "Inbound Socket Messages", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            

            string url = string.Empty;
            List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
            List<string> onOpen = new List<string>();

            DA.GetData(0, ref url);
            DA.GetDataList(1, queryParams);
            DA.GetDataList(2, onOpen);

            this.SocketMessageReceived -= this.HandleSocketMessage;
            this.SocketMessageReceived += this.HandleSocketMessage;

            
            if (string.IsNullOrEmpty(url)) throw new Exception("Invalid Url");
            string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(o => o.Value).ToList());

            _url = fullUrl;
            _queryParams = queryParams.Select(q => q.Value).ToList();
            _onOpen = onOpen;

            if (this._resetClient)
            {
                this.ClearRuntimeMessages();
                this._client = null;
                this._runningTask = Task.Run(this.ListenForRequests);
                this.lastMessage = null;
            }

            this._resetClient = true;

            DA.SetData(0, lastMessage);
        }

        public void HandleSocketMessage(object sender, EventArgs args)
        {
            Grasshopper.Instances.ActiveCanvas.BeginInvoke(new Action(() => {
                this.ExpireSolution(true);
            }));
            
        }

        public async Task ListenForRequests()
        {
            try
            {
                await this.Client.ConnectAsync(new Uri(this._url), CancellationToken.None);

                foreach (string msg in this._onOpen)
                {
                    System.ArraySegment<byte> payload = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
                    await this.Client.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
                }

                while (this.Client.State == WebSocketState.Open)
                {
                    //Receive buffer
                    var receiveBuffer = new byte[1048576]; //1 MB

                    ArraySegment<byte> bytesReceived = new ArraySegment<byte>(receiveBuffer);
                    WebSocketReceiveResult result = await Client.ReceiveAsync(bytesReceived, CancellationToken.None);

                    string message = Encoding.UTF8.GetString(receiveBuffer);
                    this.lastMessage = message;
                    this._resetClient = false;
                    this.OnSocketMessageReceived(new SocketMessageReceivedEventArgs(message));
                }
            }
            catch (Exception exc)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exc.Message);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2BAEA8DD-9496-4644-9077-67EF4910675E"); }
        }

        public EventHandler SocketMessageReceived;
        public void OnSocketMessageReceived(SocketMessageReceivedEventArgs e)
        {
            EventHandler handler = SocketMessageReceived;
            handler?.Invoke(this, e);
        }
    }
}