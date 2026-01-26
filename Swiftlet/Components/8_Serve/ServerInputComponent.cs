using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Swiftlet.Components
{
    public class ServerInputComponent : GH_Component, IGH_VariableParameterComponent
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

        // Store the most recent context for each route
        private Dictionary<string, HttpListenerContext> _pendingContexts = new Dictionary<string, HttpListenerContext>();
        private bool _requestTriggeredSolve = false;
        private int _currentPort = -1;

        public ServerInputComponent()
            : base("Server Input", "SI",
                "Listens for HTTP requests on a specified port and routes them to outputs based on the request path. " +
                "Add outputs via the component's Zoomable UI (ZUI) to handle different routes.",
                NamingUtility.CATEGORY, NamingUtility.LISTEN)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Port", "P", "Port number to listen on (0-65535)", GH_ParamAccess.item, 8080);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // Default route output - cannot be removed
            var param = new ListenerRequestParam();
            param.Name = "Root";
            param.NickName = "/";
            param.Description = "Default route for requests to /";
            param.Access = GH_ParamAccess.item;
            pManager.AddParameter(param);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int port = 8080;
            DA.GetData(0, ref port);

            if (port < 0 || port > 65535)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port number must be between 0 and 65535");
                return;
            }

            // If port changed, restart listener
            bool portChanged = _currentPort != port;

            // Stop existing listener if port changed or if this is a user-triggered solve
            if (portChanged || !_requestTriggeredSolve)
            {
                StopListener();
                _currentPort = port;
            }

            // Clear pending contexts if this is a user-triggered solve
            if (!_requestTriggeredSolve)
            {
                _pendingContexts.Clear();
            }

            // Build the message showing the base URL
            string baseUrl = $"http://localhost:{port}/";
            this.Message = baseUrl;

            // Output contexts for each route
            for (int i = 0; i < Params.Output.Count; i++)
            {
                string route = NormalizeRoute(Params.Output[i].NickName);

                if (_pendingContexts.TryGetValue(route, out HttpListenerContext context))
                {
                    DA.SetData(i, new ListenerRequestGoo(context));
                    // Remove from pending after outputting
                    _pendingContexts.Remove(route);
                }
                else
                {
                    DA.SetData(i, null);
                }
            }

            // Reset the flag
            _requestTriggeredSolve = false;

            // Subscribe to request event
            this.HttpRequestReceived -= OnHttpRequestReceived;
            this.HttpRequestReceived += OnHttpRequestReceived;

            // Start listener after solve
        }

        protected override void AfterSolveInstance()
        {
            base.AfterSolveInstance();

            // Start listening for requests
            if (!Listener.IsListening)
            {
                try
                {
                    string prefix = $"http://localhost:{_currentPort}/";
                    Listener.Prefixes.Clear();
                    Listener.Prefixes.Add(prefix);

                    this.ListenerTask = Task.Run(() => ListenForRequests());
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start listener: {ex.Message}");
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
                        // This blocks until a request is received
                        var context = Listener.GetContext();

                        if (context != null)
                        {
                            string requestPath = NormalizeRoute(context.Request.Url.AbsolutePath);

                            // Find matching route
                            string matchedRoute = FindMatchingRoute(requestPath);

                            if (matchedRoute != null)
                            {
                                // Store context (replacing any previous unprocessed one for this route)
                                _pendingContexts[matchedRoute] = context;
                                _requestTriggeredSolve = true;

                                // Trigger re-solve on UI thread
                                RaiseHttpRequestReceived(new RequestReceivedEventArgs(context));
                            }
                            else
                            {
                                // No matching route - return 404
                                try
                                {
                                    context.Response.StatusCode = 404;
                                    context.Response.StatusDescription = "Not Found";
                                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes("404 - Route not found");
                                    context.Response.ContentLength64 = buffer.Length;
                                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                                    context.Response.OutputStream.Close();
                                }
                                catch { }
                            }
                        }
                    }
                    catch (HttpListenerException)
                    {
                        // Listener was stopped
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // Listener failed to start or was stopped
            }
        }

        private string FindMatchingRoute(string requestPath)
        {
            // Collect all defined routes
            var routes = Params.Output.Select(p => NormalizeRoute(p.NickName)).ToList();

            // Exact match first
            if (routes.Contains(requestPath))
            {
                return requestPath;
            }

            // Find best prefix match (longest matching prefix)
            string bestMatch = null;
            int bestLength = 0;

            foreach (var route in routes)
            {
                if (requestPath.StartsWith(route) && route.Length > bestLength)
                {
                    bestMatch = route;
                    bestLength = route.Length;
                }
            }

            return bestMatch;
        }

        private string NormalizeRoute(string route)
        {
            if (string.IsNullOrEmpty(route))
                return "/";

            // Ensure starts with /
            if (!route.StartsWith("/"))
                route = "/" + route;

            // Remove trailing slash (except for root)
            if (route.Length > 1 && route.EndsWith("/"))
                route = route.TrimEnd('/');

            return route.ToLowerInvariant();
        }

        private void OnHttpRequestReceived(object sender, EventArgs e)
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
            return side == GH_ParameterSide.Output;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            // Can't remove the default "/" route (index 0)
            return side == GH_ParameterSide.Output && index > 0;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            var param = new ListenerRequestParam();
            param.Name = $"Route {index}";
            param.NickName = "/new-route";
            param.Description = "HTTP route endpoint";
            param.Access = GH_ParamAccess.item;
            return param;
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public void VariableParameterMaintenance()
        {
            // Validate all route names start with "/"
            foreach (var param in Params.Output)
            {
                if (!param.NickName.StartsWith("/"))
                {
                    param.NickName = "/" + param.NickName;
                }
            }
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

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_server_input;

        public override Guid ComponentGuid => new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

        #region Events

        public event EventHandler HttpRequestReceived;

        private void RaiseHttpRequestReceived(RequestReceivedEventArgs e)
        {
            HttpRequestReceived?.Invoke(this, e);
        }

        #endregion
    }
}
