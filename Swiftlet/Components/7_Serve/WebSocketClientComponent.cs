using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
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
    /// <summary>
    /// WebSocket client component that connects to a WebSocket server,
    /// receives messages, and outputs a connection context for sending messages.
    /// </summary>
    public class WebSocketClientComponent : GH_Component
    {
        private ClientWebSocket _client;
        private Task _listenerTask;
        private CancellationTokenSource _cancellationTokenSource;

        private string _fullUrl;
        private bool _run;

        private WebSocketConnection _connection;
        private string _lastMessage;
        private List<string> _messageBuffer;
        private bool _freeze;

        public WebSocketClientComponent()
          : base("WebSocket Client", "WS Client",
              "Connects to a WebSocket server and receives messages.\nOutputs a Connection that can be used with WebSocket Send to send messages.",
              NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
            _messageBuffer = new List<string>();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "URL of WebSocket server (ws:// or wss://)", GH_ParamAccess.item);
            pManager.AddParameter(new QueryParamParam(), "Params", "P", "Query parameters to append to the URL", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "R", "Set to true to connect, false to disconnect", GH_ParamAccess.item, false);

            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new WebSocketConnectionParam(), "Connection", "C", "WebSocket connection for sending messages via WebSocket Send component", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "M", "Most recent message received from the server", GH_ParamAccess.item);
            pManager.AddTextParameter("Messages", "Ms", "All messages received since last solve", GH_ParamAccess.list);
            pManager.AddTextParameter("Status", "S", "Connection status", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            _freeze = true;
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string url = string.Empty;
            List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
            bool run = false;

            DA.GetData(0, ref url);
            DA.GetDataList(1, queryParams);
            DA.GetData(2, ref run);

            this.SocketMessageReceived -= HandleSocketMessage;
            this.SocketMessageReceived += HandleSocketMessage;

            if (string.IsNullOrEmpty(url))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "URL cannot be empty");
                DA.SetData(3, "Error: No URL");
                return;
            }

            string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(o => o.Value).ToList());
            bool urlChanged = _fullUrl != fullUrl;

            _fullUrl = fullUrl;

            // Handle run state changes
            if (run && !_run)
            {
                // Starting connection
                StopListener();
                _messageBuffer.Clear();
                _lastMessage = null;
                _connection = null;
                StartListener();
            }
            else if (!run && _run)
            {
                // Stopping connection
                StopListener();
                _connection = null;
            }
            else if (run && urlChanged && IsConnected())
            {
                // URL changed while running - reconnect
                StopListener();
                _messageBuffer.Clear();
                _connection = null;
                StartListener();
            }

            _run = run;

            // Output status
            string status = _connection?.GetStatusString() ?? "Disconnected";
            this.Message = status;

            // Copy buffer to output and clear
            List<string> outputMessages = new List<string>(_messageBuffer);
            _messageBuffer.Clear();

            // Output connection context (allows downstream WebSocket Send to use this connection)
            if (_connection != null)
            {
                DA.SetData(0, new WebSocketConnectionGoo(_connection));
            }

            DA.SetData(1, _lastMessage);
            DA.SetDataList(2, outputMessages);
            DA.SetData(3, status);
        }

        protected override void AfterSolveInstance()
        {
            _freeze = false;
            base.AfterSolveInstance();
        }

        private bool IsConnected()
        {
            return _client != null && _client.State == WebSocketState.Open;
        }

        private void StartListener()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _client = new ClientWebSocket();
                _listenerTask = Task.Run(() => ListenForMessages(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start: {ex.Message}");
            }
        }

        private void StopListener()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                if (_client != null && _client.State == WebSocketState.Open)
                {
                    try
                    {
                        var closeTask = _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        closeTask.Wait(1000);
                    }
                    catch
                    {
                        // Ignore close errors
                    }
                }

                _client?.Dispose();
                _client = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private void HandleSocketMessage(object sender, EventArgs args)
        {
            if (!_freeze)
            {
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    ExpireSolution(true);
                });
            }
        }

        private async Task ListenForMessages(CancellationToken cancellationToken)
        {
            try
            {
                await _client.ConnectAsync(new Uri(_fullUrl), cancellationToken);

                // Create the connection context now that we're connected
                _connection = new WebSocketConnection(
                    _client,
                    isServer: false,
                    remoteEndpoint: _fullUrl,
                    localEndpoint: "client"
                );

                // Trigger solve to update status and output connection
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    ExpireSolution(true);
                });

                // Listen for incoming messages
                var buffer = new byte[8192];
                var messageBuilder = new StringBuilder();

                while (!cancellationToken.IsCancellationRequested && _client.State == WebSocketState.Open)
                {
                    messageBuilder.Clear();
                    WebSocketReceiveResult result;

                    do
                    {
                        var segment = new ArraySegment<byte>(buffer);
                        result = await _client.ReceiveAsync(segment, cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }

                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    string message = messageBuilder.ToString();
                    _lastMessage = message;
                    _messageBuffer.Add(message);
                    OnSocketMessageReceived(new SocketMessageReceivedEventArgs(message));
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }
            catch (WebSocketException)
            {
                // Connection closed
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
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

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.Icons_socket_listener_24x24; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D"); }
        }

        public EventHandler SocketMessageReceived;
        public void OnSocketMessageReceived(SocketMessageReceivedEventArgs e)
        {
            EventHandler handler = SocketMessageReceived;
            handler?.Invoke(this, e);
        }
    }
}
