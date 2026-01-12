using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Swiftlet.Components._7_Serve
{
    public class UdpListener : GH_Component
    {
        private UdpClient _client;
        private Task _listenerTask;
        private CancellationTokenSource _cancellationTokenSource;

        private int _port;
        private byte[] _lastData;
        private string _lastRemoteEndPoint;
        private bool _freeze;

        /// <summary>
        /// Initializes a new instance of the UdpListener class.
        /// </summary>
        public UdpListener()
          : base("UDP Listener", "UDP-L",
              "A UDP listener component that receives UDP datagrams on a specified port.\nThis component is ALPHA. Use at your own risk.",
              NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Port", "P", "Port number to listen on (0-65535)", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ByteArrayParam(), "Data", "D", "Received data as byte array", GH_ParamAccess.item);
            pManager.AddTextParameter("Remote", "R", "Remote endpoint (IP:Port) that sent the data", GH_ParamAccess.item);
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
            int port = 0;
            DA.GetData(0, ref port);

            if (port < 0 || port > 65535)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port must be between 0 and 65535");
                return;
            }

            this.UdpDataReceived -= HandleUdpDataReceived;
            this.UdpDataReceived += HandleUdpDataReceived;

            bool portChanged = this._port != port;
            this._port = port;

            this.Message = $":{port}";

            if (_client == null || portChanged)
            {
                StopListener();
                StartListener(port);
            }

            if (_lastData != null)
            {
                DA.SetData(0, new ByteArrayGoo(_lastData));
            }
            DA.SetData(1, _lastRemoteEndPoint);
        }

        protected override void AfterSolveInstance()
        {
            this._freeze = false;
            base.AfterSolveInstance();
        }

        private void StartListener(int port)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _client = new UdpClient(port);
                _listenerTask = Task.Run(() => ListenForData(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start listener: {ex.Message}");
            }
        }

        private void StopListener()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _client?.Close();
                _client = null;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private async Task ListenForData(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _client.ReceiveAsync();
                    _lastData = result.Buffer;
                    _lastRemoteEndPoint = $"{result.RemoteEndPoint.Address}:{result.RemoteEndPoint.Port}";
                    OnUdpDataReceived(new UdpDataReceivedEventArgs(result.Buffer, result.RemoteEndPoint));
                }
            }
            catch (ObjectDisposedException)
            {
                // Client was closed, this is expected during cleanup
            }
            catch (SocketException)
            {
                // Socket was closed, this is expected during cleanup
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                });
            }
        }

        private void HandleUdpDataReceived(object sender, EventArgs args)
        {
            if (!this._freeze)
            {
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    ExpireSolution(true);
                });
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            StopListener();
            base.RemovedFromDocument(document);
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Close)
            {
                StopListener();
            }
            base.DocumentContextChanged(document, context);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("B4E8C3D2-9F5A-4E6B-8C7D-2F3E4A5B6C7D"); }
        }

        public EventHandler UdpDataReceived;
        public void OnUdpDataReceived(UdpDataReceivedEventArgs e)
        {
            EventHandler handler = UdpDataReceived;
            handler?.Invoke(this, e);
        }
    }
}
