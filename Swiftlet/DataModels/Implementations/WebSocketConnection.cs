using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Implementations
{
    /// <summary>
    /// Represents a WebSocket connection that can be used to send messages.
    /// This context is passed through Grasshopper's data flow and can be used
    /// by the WebSocket Send component to send messages through an open connection.
    /// </summary>
    public class WebSocketConnection
    {
        private readonly object _sendLock = new object();

        /// <summary>
        /// The underlying WebSocket instance.
        /// </summary>
        public WebSocket WebSocket { get; private set; }

        /// <summary>
        /// Unique identifier for this connection.
        /// </summary>
        public string ConnectionId { get; private set; }

        /// <summary>
        /// True if this is a server-side connection (accepting clients), false if client-side.
        /// </summary>
        public bool IsServer { get; private set; }

        /// <summary>
        /// The remote endpoint information (for server connections, identifies the client).
        /// </summary>
        public string RemoteEndpoint { get; private set; }

        /// <summary>
        /// The local endpoint (URL for client, port for server).
        /// </summary>
        public string LocalEndpoint { get; private set; }

        /// <summary>
        /// When the connection was established.
        /// </summary>
        public DateTime ConnectedAt { get; private set; }

        /// <summary>
        /// Creates a new WebSocket connection context.
        /// </summary>
        public WebSocketConnection(
            WebSocket webSocket,
            bool isServer,
            string remoteEndpoint,
            string localEndpoint)
        {
            this.WebSocket = webSocket;
            this.ConnectionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            this.IsServer = isServer;
            this.RemoteEndpoint = remoteEndpoint;
            this.LocalEndpoint = localEndpoint;
            this.ConnectedAt = DateTime.Now;
        }

        /// <summary>
        /// Gets the current state of the WebSocket connection.
        /// </summary>
        public WebSocketState State
        {
            get
            {
                if (WebSocket == null) return WebSocketState.None;
                return WebSocket.State;
            }
        }

        /// <summary>
        /// Returns true if the connection is open and can send/receive messages.
        /// </summary>
        public bool IsOpen
        {
            get { return WebSocket != null && WebSocket.State == WebSocketState.Open; }
        }

        /// <summary>
        /// Sends a text message through the WebSocket connection.
        /// Thread-safe - multiple components can send through the same connection.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>True if the message was sent successfully.</returns>
        public bool SendMessage(string message)
        {
            if (!IsOpen || string.IsNullOrEmpty(message))
                return false;

            lock (_sendLock)
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    var segment = new ArraySegment<byte>(buffer);
                    var task = WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    task.Wait(5000); // 5 second timeout
                    return task.IsCompleted && !task.IsFaulted;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Sends binary data through the WebSocket connection.
        /// Thread-safe - multiple components can send through the same connection.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <returns>True if the data was sent successfully.</returns>
        public bool SendBinary(byte[] data)
        {
            if (!IsOpen || data == null || data.Length == 0)
                return false;

            lock (_sendLock)
            {
                try
                {
                    var segment = new ArraySegment<byte>(data);
                    var task = WebSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
                    task.Wait(5000); // 5 second timeout
                    return task.IsCompleted && !task.IsFaulted;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a human-readable status string.
        /// </summary>
        public string GetStatusString()
        {
            if (WebSocket == null) return "No connection";

            switch (WebSocket.State)
            {
                case WebSocketState.None:
                    return "Not connected";
                case WebSocketState.Connecting:
                    return "Connecting...";
                case WebSocketState.Open:
                    return "Connected";
                case WebSocketState.CloseSent:
                    return "Closing...";
                case WebSocketState.CloseReceived:
                    return "Remote closing...";
                case WebSocketState.Closed:
                    return "Closed";
                case WebSocketState.Aborted:
                    return "Aborted";
                default:
                    return "Unknown";
            }
        }

        public override string ToString()
        {
            string type = IsServer ? "Server" : "Client";
            string status = GetStatusString();
            return $"WebSocket {type} [{ConnectionId}] - {status}";
        }
    }
}
