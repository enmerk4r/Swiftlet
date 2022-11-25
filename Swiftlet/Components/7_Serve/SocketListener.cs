using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private string _fullUrl;
        private List<string> _onOpen;

        private string lastMessage;

        private CancellationToken _cancellationToken;

        private bool _freeze;

        private int _failCounter;


        /// <summary>
        /// Initializes a new instance of the SocketListener class.
        /// </summary>
        public SocketListener()
          : base("Socket Listener", "SOCKET",
              "A simple socket listener component. \nThis component is VERY ALPHA! Use at your own risk.\nIf you are having trouble stopping it from running, just disconnect from the internet and wait for a few seconds.",
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

        protected override void BeforeSolveInstance()
        {
            this._freeze = true;
            base.BeforeSolveInstance();
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

            bool inputsChanged = this.InputsChanged(fullUrl, onOpen);

            _fullUrl = fullUrl;
            _onOpen = onOpen;

            if (this.Client?.State != WebSocketState.Open || inputsChanged)
            {
                if (_failCounter < 5)
                {
                    _cancellationToken = new CancellationTokenSource().Token;
                    this.ClearRuntimeMessages();
                    this._client = null;
                    this._runningTask = Task.Run(this.ListenForRequests);
                    this.lastMessage = null;
                }
                else
                {
                    _failCounter = 0;
                }
            }

            DA.SetData(0, lastMessage);
        }

        protected override void AfterSolveInstance()
        {
            this._freeze = false;
            base.AfterSolveInstance();
        }

        public bool InputsChanged(string fullUrl, List<string> onOpen)
        {
            if (this._fullUrl != fullUrl) return true;
            if (this._onOpen != null && onOpen != null)
            {
                if (this._onOpen.Count != onOpen.Count) return true;
                for (int i = 0; i < onOpen.Count; i++)
                {
                    if (this._onOpen[i] != onOpen[i]) return true;
                }
            }
            if (this._onOpen == null && onOpen != null) return true;
            if (this._onOpen != null && onOpen == null) return true;
            return false;
        }

        public void HandleSocketMessage(object sender, EventArgs args)
        {
            if (!this._freeze)
            {
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    ExpireSolution(true);
                });
            }
        }

        public async Task ListenForRequests()
        {
            try
            {
                await this.Client.ConnectAsync(new Uri(this._fullUrl), _cancellationToken);

                foreach (string msg in this._onOpen)
                {
                    System.ArraySegment<byte> payload = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
                    await this.Client.SendAsync(payload, WebSocketMessageType.Text, true, _cancellationToken);
                }

                while (this.Client.State == WebSocketState.Open)
                {
                    //Receive buffer
                    var receiveBuffer = new byte[1024]; //1 MB

                    ArraySegment<byte> bytesReceived = new ArraySegment<byte>(receiveBuffer);
                    WebSocketReceiveResult result = await Client.ReceiveAsync(bytesReceived, _cancellationToken);

                    string message = Encoding.UTF8.GetString(receiveBuffer);
                    this.lastMessage = message.Replace("\0", "");
                    this.OnSocketMessageReceived(new SocketMessageReceivedEventArgs(message));
                }
            }
            catch (Exception exc)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exc.Message);
                this._failCounter++;
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
                return Properties.Resources.Icons_socket_listener_24x24;
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