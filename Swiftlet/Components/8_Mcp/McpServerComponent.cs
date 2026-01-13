using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Components
{
    public class McpServerComponent : GH_Component, IGH_VariableParameterComponent
    {
        private HttpListener _listener;
        public HttpListener Listener
        {
            get
            {
                if (_listener == null)
                {
                    _listener = new HttpListener();
                }
                return _listener;
            }
        }

        public Task ListenerTask { get; set; }

        // Session management
        private Dictionary<string, McpSession> _sessions = new Dictionary<string, McpSession>();

        // Store pending tool calls by tool name
        private Dictionary<string, McpToolCallContext> _pendingCalls = new Dictionary<string, McpToolCallContext>();

        // Tool definitions from input
        private List<McpToolDefinition> _tools = new List<McpToolDefinition>();

        private bool _requestTriggeredSolve = false;
        private int _currentPort = -1;
        private string _serverName = "Grasshopper MCP Server";

        private const string MCP_PROTOCOL_VERSION = "2024-11-05";

        public McpServerComponent()
            : base("MCP Server", "MCP",
                "An MCP (Model Context Protocol) server that exposes Grasshopper tools to AI clients like Claude.",
                NamingUtility.CATEGORY, NamingUtility.MCP)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Port", "P", "Port number to listen on (default: 3001)", GH_ParamAccess.item, 3001);
            pManager.AddParameter(new McpToolDefinitionParam(), "Tools", "T", "Tool definitions to expose", GH_ParamAccess.list);
            pManager.AddTextParameter("Server Name", "N", "Server name for MCP protocol", GH_ParamAccess.item, "Swiftlet");

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // No default outputs - they are created dynamically based on tool definitions
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int port = 3001;
            List<McpToolDefinitionGoo> toolGoos = new List<McpToolDefinitionGoo>();
            string serverName = "Grasshopper MCP Server";

            DA.GetData(0, ref port);
            DA.GetDataList(1, toolGoos);
            DA.GetData(2, ref serverName);

            if (port < 0 || port > 65535)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port number must be between 0 and 65535");
                return;
            }

            _serverName = serverName;

            // Extract tools from Goo wrappers
            _tools = toolGoos
                .Where(g => g?.Value != null)
                .Select(g => g.Value)
                .ToList();

            // Update outputs based on tool definitions
            // If outputs changed, return early - the DA object doesn't know about the new outputs
            // and a new solve will be triggered with the correct structure
            if (UpdateOutputsFromTools())
            {
                return;
            }

            // If port changed, restart listener
            bool portChanged = _currentPort != port;

            if (portChanged || !_requestTriggeredSolve)
            {
                StopListener();
                _currentPort = port;
            }

            // Clear pending calls if this is a user-triggered solve
            if (!_requestTriggeredSolve)
            {
                _pendingCalls.Clear();
            }

            // Build the message showing the URL
            string url = $"http://localhost:{port}/mcp";
            this.Message = url;

            // Output pending calls for each tool
            for (int i = 0; i < Params.Output.Count; i++)
            {
                string toolName = Params.Output[i].NickName;

                if (_pendingCalls.TryGetValue(toolName, out McpToolCallContext context))
                {
                    DA.SetData(i, new McpToolCallRequestGoo(context));
                    _pendingCalls.Remove(toolName);
                }
                else
                {
                    DA.SetData(i, null);
                }
            }

            _requestTriggeredSolve = false;

            // Subscribe to request event
            this.McpRequestReceived -= OnMcpRequestReceived;
            this.McpRequestReceived += OnMcpRequestReceived;
        }

        /// <summary>
        /// Updates output parameters based on tool definitions.
        /// </summary>
        /// <returns>True if outputs were modified, false otherwise.</returns>
        private bool UpdateOutputsFromTools()
        {
            // Get current output tool names
            var currentOutputs = new HashSet<string>(Params.Output.Select(p => p.NickName));
            var desiredOutputs = new HashSet<string>(_tools.Select(t => t.Name));

            bool changed = false;

            // Remove outputs that are no longer needed
            for (int i = Params.Output.Count - 1; i >= 0; i--)
            {
                if (!desiredOutputs.Contains(Params.Output[i].NickName))
                {
                    Params.UnregisterOutputParameter(Params.Output[i]);
                    changed = true;
                }
            }

            // Add new outputs for tools that don't have them
            foreach (var tool in _tools)
            {
                if (!currentOutputs.Contains(tool.Name))
                {
                    var param = new McpToolCallRequestParam();
                    param.Name = tool.Name;
                    param.NickName = tool.Name;
                    param.Description = $"Tool call context for '{tool.Name}'";
                    param.Access = GH_ParamAccess.item;
                    Params.RegisterOutputParam(param);
                    changed = true;
                }
            }

            if (changed)
            {
                Params.OnParametersChanged();
            }

            return changed;
        }

        protected override void AfterSolveInstance()
        {
            base.AfterSolveInstance();

            if (!Listener.IsListening && _currentPort > 0)
            {
                try
                {
                    string prefix = $"http://localhost:{_currentPort}/mcp/";
                    Listener.Prefixes.Clear();
                    Listener.Prefixes.Add(prefix);

                    ListenerTask = Task.Run(() => ListenForRequests());
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start MCP server: {ex.Message}");
                }
            }
        }

        private void ListenForRequests()
        {
            try
            {
                Listener.Start();

                while (Listener.IsListening)
                {
                    try
                    {
                        var context = Listener.GetContext();
                        if (context != null)
                        {
                            Task.Run(() => HandleRequest(context));
                        }
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // Listener failed to start or was stopped
            }
        }

        private void HandleRequest(HttpListenerContext httpContext)
        {
            try
            {
                var request = httpContext.Request;
                var response = httpContext.Response;

                // Only handle POST requests
                if (request.HttpMethod != "POST")
                {
                    SendError(response, 405, "Method Not Allowed");
                    return;
                }

                // Read request body
                string body;
                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    body = reader.ReadToEnd();
                }

                JObject jsonRpc;
                try
                {
                    jsonRpc = JObject.Parse(body);
                }
                catch
                {
                    SendJsonRpcError(response, null, -32700, "Parse error");
                    return;
                }

                string method = jsonRpc["method"]?.ToString();
                object id = jsonRpc["id"]?.ToObject<object>();
                JToken paramsToken = jsonRpc["params"];

                // Get session ID from header
                string sessionId = request.Headers["Mcp-Session-Id"];

                switch (method)
                {
                    case "initialize":
                        HandleInitialize(httpContext, id, paramsToken);
                        break;

                    case "notifications/initialized":
                    case "initialized":
                        HandleInitialized(httpContext, sessionId);
                        break;

                    case "tools/list":
                        HandleToolsList(httpContext, id, sessionId);
                        break;

                    case "tools/call":
                        HandleToolsCall(httpContext, id, paramsToken, sessionId);
                        break;

                    case "ping":
                        SendJsonRpcResult(response, id, new JObject());
                        break;

                    default:
                        SendJsonRpcError(response, id, -32601, $"Method not found: {method}");
                        break;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    SendError(httpContext.Response, 500, ex.Message);
                }
                catch { }
            }
        }

        private void HandleInitialize(HttpListenerContext httpContext, object id, JToken paramsToken)
        {
            // Create new session
            string sessionId = Guid.NewGuid().ToString();
            _sessions[sessionId] = new McpSession { Id = sessionId, Created = DateTime.UtcNow, Initialized = false };

            var result = new JObject
            {
                ["protocolVersion"] = MCP_PROTOCOL_VERSION,
                ["capabilities"] = new JObject
                {
                    ["tools"] = new JObject()
                },
                ["serverInfo"] = new JObject
                {
                    ["name"] = _serverName,
                    ["version"] = "1.0.0"
                }
            };

            var response = httpContext.Response;
            response.Headers["Mcp-Session-Id"] = sessionId;
            SendJsonRpcResult(response, id, result);
        }

        private void HandleInitialized(HttpListenerContext httpContext, string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId) && _sessions.TryGetValue(sessionId, out var session))
            {
                session.Initialized = true;
            }

            httpContext.Response.StatusCode = 202;
            httpContext.Response.Close();
        }

        private void HandleToolsList(HttpListenerContext httpContext, object id, string sessionId)
        {
            var toolsArray = new JArray();
            foreach (var tool in _tools)
            {
                toolsArray.Add(tool.ToJson());
            }

            var result = new JObject
            {
                ["tools"] = toolsArray
            };

            SendJsonRpcResult(httpContext.Response, id, result);
        }

        private void HandleToolsCall(HttpListenerContext httpContext, object id, JToken paramsToken, string sessionId)
        {
            string toolName = paramsToken?["name"]?.ToString();
            JObject arguments = paramsToken?["arguments"] as JObject ?? new JObject();

            if (string.IsNullOrEmpty(toolName))
            {
                SendJsonRpcError(httpContext.Response, id, -32602, "Missing tool name");
                return;
            }

            // Check if tool exists
            var tool = _tools.FirstOrDefault(t => t.Name == toolName);
            if (tool == null)
            {
                SendJsonRpcError(httpContext.Response, id, -32602, $"Unknown tool: {toolName}");
                return;
            }

            // Create call context
            var callContext = new McpToolCallContext(sessionId, id, toolName, arguments, httpContext);

            // Store pending call (replacing any previous unprocessed one for this tool)
            _pendingCalls[toolName] = callContext;
            _requestTriggeredSolve = true;

            // Trigger re-solve on UI thread
            RaiseMcpRequestReceived();
        }

        private void SendJsonRpcResult(HttpListenerResponse response, object id, JToken result)
        {
            var jsonResponse = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id != null ? JToken.FromObject(id) : JValue.CreateNull(),
                ["result"] = result
            };

            WriteJsonResponse(response, 200, jsonResponse);
        }

        private void SendJsonRpcError(HttpListenerResponse response, object id, int code, string message)
        {
            var jsonResponse = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id != null ? JToken.FromObject(id) : JValue.CreateNull(),
                ["error"] = new JObject
                {
                    ["code"] = code,
                    ["message"] = message
                }
            };

            WriteJsonResponse(response, 200, jsonResponse);
        }

        private void SendError(HttpListenerResponse response, int statusCode, string message)
        {
            response.StatusCode = statusCode;
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private void WriteJsonResponse(HttpListenerResponse response, int statusCode, JObject json)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";

            byte[] buffer = Encoding.UTF8.GetBytes(json.ToString());
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private void OnMcpRequestReceived(object sender, EventArgs e)
        {
            Grasshopper.Instances.ActiveCanvas?.BeginInvoke(new Action(() =>
            {
                this.ExpireSolution(true);
            }));
        }

        private void StopListener()
        {
            try
            {
                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                }
                _listener?.Close();
                _listener = null;
                ListenerTask?.Dispose();
                ListenerTask = null;
            }
            catch { }
        }

        #region IGH_VariableParameterComponent

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            // Outputs are managed automatically based on tool definitions
            return false;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            // Outputs are managed automatically based on tool definitions
            return false;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            var param = new McpToolCallRequestParam();
            param.Name = $"Tool {index}";
            param.NickName = $"tool_{index}";
            param.Description = "Tool call context";
            param.Access = GH_ParamAccess.item;
            return param;
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public void VariableParameterMaintenance()
        {
            // Nothing to maintain
        }

        #endregion

        #region Lifecycle

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

        #endregion

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("D4E5F6A7-B8C9-0123-DEF0-234567890123");

        #region Events

        public event EventHandler McpRequestReceived;

        private void RaiseMcpRequestReceived()
        {
            McpRequestReceived?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        private class McpSession
        {
            public string Id { get; set; }
            public DateTime Created { get; set; }
            public bool Initialized { get; set; }
        }
    }
}
