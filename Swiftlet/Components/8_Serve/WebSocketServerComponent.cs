using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Swiftlet.Components._7_Serve
{
    /// <summary>
    /// WebSocket server component that accepts incoming WebSocket connections.
    /// Outputs connection contexts that can be used with WebSocket Send to respond to clients.
    /// </summary>
    public class WebSocketServerComponent : GH_Component
    {
        private HttpListener _httpListener;
        private Task _listenerTask;
        private CancellationTokenSource _cancellationTokenSource;

        private int _port;
        private bool _run;
        private bool _freeze;

        // Track active connections
        private ConcurrentDictionary<string, WebSocketConnection> _activeConnections;
        private ConcurrentDictionary<string, Task> _clientTasks;

        // Message queue for received messages
        private ConcurrentQueue<ReceivedMessage> _messageQueue;

        // Current message to output (most recent)
        private string _lastMessage;
        private WebSocketConnection _lastConnection;

        public WebSocketServerComponent()
          : base("WebSocket Server", "WS Server",
              "Accepts WebSocket connections from clients.\nOutputs Connection for each message received, use with WebSocket Send to respond.",
              NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
            _activeConnections = new ConcurrentDictionary<string, WebSocketConnection>();
            _clientTasks = new ConcurrentDictionary<string, Task>();
            _messageQueue = new ConcurrentQueue<ReceivedMessage>();
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Port", "P", "Port number to listen on (0-65535)", GH_ParamAccess.item, 8181);
            pManager.AddBooleanParameter("Run", "R", "Set to true to start the server, false to stop", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new WebSocketConnectionParam(), "Connection", "C", "Connection context for the client that sent the message (use with WebSocket Send)", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "M", "Most recent message received", GH_ParamAccess.item);
            pManager.AddTextParameter("Messages", "Ms", "All messages received since last solve", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Clients", "N", "Number of connected clients", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Server status", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            _freeze = true;
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int port = 8181;
            bool run = false;

            DA.GetData(0, ref port);
            DA.GetData(1, ref run);

            if (port < 0 || port > 65535)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port must be between 0 and 65535");
                DA.SetData(4, "Error: Invalid port");
                return;
            }

            this.WebSocketMessageReceived -= HandleWebSocketMessage;
            this.WebSocketMessageReceived += HandleWebSocketMessage;

            bool portChanged = _port != port;
            _port = port;

            // Handle run state changes
            if (run && !_run)
            {
                // Starting server
                StopServer();
                StartServer(port);
            }
            else if (!run && _run)
            {
                // Stopping server
                StopServer();
            }
            else if (run && portChanged)
            {
                // Port changed while running - restart
                StopServer();
                StartServer(port);
            }

            _run = run;

            // Collect all queued messages
            var messages = new List<string>();
            while (_messageQueue.TryDequeue(out ReceivedMessage msg))
            {
                messages.Add(msg.Message);
                _lastMessage = msg.Message;
                _lastConnection = msg.Connection;
            }

            // Output status
            string status = _run ? $"Listening on port {port}" : "Stopped";
            int clientCount = _activeConnections.Count;
            this.Message = _run ? $":{port} ({clientCount} clients)" : "Stopped";

            // Output connection context for the most recent message
            if (_lastConnection != null)
            {
                DA.SetData(0, new WebSocketConnectionGoo(_lastConnection));
            }

            DA.SetData(1, _lastMessage);
            DA.SetDataList(2, messages);
            DA.SetData(3, clientCount);
            DA.SetData(4, status);
        }

        protected override void AfterSolveInstance()
        {
            _freeze = false;
            base.AfterSolveInstance();
        }

        private void StartServer(int port)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{port}/");
                _httpListener.Start();
                _listenerTask = Task.Run(() => AcceptConnections(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start server: {ex.Message}");
            }
        }

        private void StopServer()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                // Close all active connections
                foreach (var kvp in _activeConnections)
                {
                    try
                    {
                        var ws = kvp.Value.WebSocket;
                        if (ws.State == WebSocketState.Open)
                        {
                            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", CancellationToken.None).Wait(1000);
                        }
                    }
                    catch
                    {
                        // Ignore close errors
                    }
                }
                _activeConnections.Clear();
                _clientTasks.Clear();

                _httpListener?.Stop();
                _httpListener?.Close();
                _httpListener = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private async Task AcceptConnections(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await _httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        var webSocket = wsContext.WebSocket;
                        var remoteEndpoint = context.Request.RemoteEndPoint?.ToString() ?? "unknown";

                        var connection = new WebSocketConnection(
                            webSocket,
                            isServer: true,
                            remoteEndpoint: remoteEndpoint,
                            localEndpoint: $":{_port}"
                        );

                        _activeConnections[connection.ConnectionId] = connection;

                        // Start a task to handle this client's messages
                        var clientTask = Task.Run(() => HandleClient(connection, cancellationToken));
                        _clientTasks[connection.ConnectionId] = clientTask;

                        // Trigger solve to update client count
                        TriggerSolve();
                    }
                    else
                    {
                        // Not a WebSocket request - send 400 Bad Request
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (HttpListenerException)
            {
                // Listener was stopped
            }
            catch (ObjectDisposedException)
            {
                // Listener was disposed
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                });
            }
        }

        private async Task HandleClient(WebSocketConnection connection, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            var messageBuilder = new StringBuilder();

            try
            {
                while (!cancellationToken.IsCancellationRequested && connection.IsOpen)
                {
                    messageBuilder.Clear();
                    WebSocketReceiveResult result;

                    do
                    {
                        var segment = new ArraySegment<byte>(buffer);
                        result = await connection.WebSocket.ReceiveAsync(segment, cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // Client requested close
                            RemoveConnection(connection.ConnectionId);
                            return;
                        }

                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    string message = messageBuilder.ToString();
                    _messageQueue.Enqueue(new ReceivedMessage(connection, message));
                    OnWebSocketMessageReceived(EventArgs.Empty);
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
            catch
            {
                // Connection error
            }
            finally
            {
                RemoveConnection(connection.ConnectionId);
            }
        }

        private void RemoveConnection(string connectionId)
        {
            _activeConnections.TryRemove(connectionId, out _);
            _clientTasks.TryRemove(connectionId, out _);
            TriggerSolve();
        }

        private void TriggerSolve()
        {
            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
            {
                ExpireSolution(true);
            });
        }

        private void HandleWebSocketMessage(object sender, EventArgs args)
        {
            if (!_freeze)
            {
                TriggerSolve();
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            StopServer();
            base.RemovedFromDocument(document);
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Close)
            {
                StopServer();
            }
            base.DocumentContextChanged(document, context);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.Icons_socket_server; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("F6A7B8C9-D0E1-4F2A-3B4C-5D6E7F8A9B0C"); }
        }

        public EventHandler WebSocketMessageReceived;
        public void OnWebSocketMessageReceived(EventArgs e)
        {
            EventHandler handler = WebSocketMessageReceived;
            handler?.Invoke(this, e);
        }

        private class ReceivedMessage
        {
            public WebSocketConnection Connection { get; }
            public string Message { get; }

            public ReceivedMessage(WebSocketConnection connection, string message)
            {
                Connection = connection;
                Message = message;
            }
        }
    }
}
