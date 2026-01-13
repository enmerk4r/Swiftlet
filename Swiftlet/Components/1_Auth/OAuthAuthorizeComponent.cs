using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Grasshopper.Kernel;
using Swiftlet.Util;

namespace Swiftlet.Components._1_Auth
{
    /// <summary>
    /// OAuth 2.0 Authorization component that handles the authorization code flow with PKCE.
    /// Opens browser for user authentication and receives the callback on localhost.
    /// </summary>
    public class OAuthAuthorizeComponent : GH_Component
    {
        private HttpListener _listener;
        private Task _listenerTask;
        private CancellationTokenSource _cancellationTokenSource;

        private string _authorizationCode;
        private string _returnedState;
        private string _error;
        private string _codeVerifier;
        private string _generatedState;
        private bool _waiting;
        private bool _completed;

        public OAuthAuthorizeComponent()
          : base("OAuth Authorize", "OAuth",
              "Initiates OAuth 2.0 Authorization Code flow with PKCE.\nOpens browser for user authentication and receives the authorization code via localhost callback.",
              NamingUtility.CATEGORY, NamingUtility.AUTH)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Auth URL", "U", "Authorization endpoint URL (e.g., https://accounts.google.com/o/oauth2/v2/auth)", GH_ParamAccess.item);
            pManager.AddTextParameter("Client ID", "ID", "OAuth Client ID from your app registration", GH_ParamAccess.item);
            pManager.AddTextParameter("Scopes", "S", "OAuth scopes to request (space-separated or as list)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Port", "P", "Localhost port for callback (default: 8888)", GH_ParamAccess.item, 8888);
            pManager.AddBooleanParameter("Authorize", "A", "Set to true to start authorization flow", GH_ParamAccess.item, false);

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "C", "Authorization code (use with OAuth Token component)", GH_ParamAccess.item);
            pManager.AddTextParameter("Code Verifier", "V", "PKCE code verifier (use with OAuth Token component)", GH_ParamAccess.item);
            pManager.AddTextParameter("Redirect URI", "R", "Redirect URI used (use with OAuth Token component)", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "St", "Current status or error message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string authUrl = string.Empty;
            string clientId = string.Empty;
            List<string> scopes = new List<string>();
            int port = 8888;
            bool authorize = false;

            DA.GetData(0, ref authUrl);
            DA.GetData(1, ref clientId);
            DA.GetDataList(2, scopes);
            DA.GetData(3, ref port);
            DA.GetData(4, ref authorize);

            string redirectUri = $"http://localhost:{port}/callback/";

            // Validate inputs
            if (string.IsNullOrEmpty(authUrl))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Authorization URL is required");
                DA.SetData(3, "Error: No authorization URL");
                return;
            }

            if (string.IsNullOrEmpty(clientId))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Client ID is required");
                DA.SetData(3, "Error: No client ID");
                return;
            }

            if (port < 1 || port > 65535)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port must be between 1 and 65535");
                DA.SetData(3, "Error: Invalid port");
                return;
            }

            // Handle authorization trigger
            if (authorize && !_waiting && !_completed)
            {
                StartAuthorizationFlow(authUrl, clientId, scopes, port, redirectUri);
            }
            else if (!authorize)
            {
                // Reset when authorize is turned off
                if (_waiting || _completed)
                {
                    StopListener();
                    _authorizationCode = null;
                    _codeVerifier = null;
                    _error = null;
                    _completed = false;
                    _waiting = false;
                }
            }

            // Update status message
            string status;
            if (_completed && !string.IsNullOrEmpty(_authorizationCode))
            {
                status = "Authorization successful";
                this.Message = "Authorized";
            }
            else if (_completed && !string.IsNullOrEmpty(_error))
            {
                status = $"Error: {_error}";
                this.Message = "Error";
            }
            else if (_waiting)
            {
                status = "Waiting for authorization... (check your browser)";
                this.Message = "Waiting...";
            }
            else
            {
                status = "Ready (set Authorize to true to start)";
                this.Message = "";
            }

            DA.SetData(0, _authorizationCode);
            DA.SetData(1, _codeVerifier);
            DA.SetData(2, redirectUri);
            DA.SetData(3, status);
        }

        private void StartAuthorizationFlow(string authUrl, string clientId, List<string> scopes, int port, string redirectUri)
        {
            try
            {
                // Generate PKCE code verifier and challenge
                _codeVerifier = GenerateCodeVerifier();
                string codeChallenge = GenerateCodeChallenge(_codeVerifier);

                // Generate state for CSRF protection
                _generatedState = GenerateState();

                // Build authorization URL
                string scopeString = string.Join(" ", scopes);
                string fullAuthUrl = BuildAuthorizationUrl(authUrl, clientId, redirectUri, scopeString, _generatedState, codeChallenge);

                // Start listening for callback
                _cancellationTokenSource = new CancellationTokenSource();
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{port}/callback/");
                _listener.Start();
                _waiting = true;

                _listenerTask = Task.Run(() => WaitForCallback(_cancellationTokenSource.Token));

                // Open browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = fullAuthUrl,
                    UseShellExecute = true
                });

                // Trigger UI update
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    ExpireSolution(true);
                });
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _completed = true;
                _waiting = false;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            }
        }

        private string BuildAuthorizationUrl(string authUrl, string clientId, string redirectUri, string scope, string state, string codeChallenge)
        {
            var queryParams = new List<string>
            {
                $"response_type=code",
                $"client_id={Uri.EscapeDataString(clientId)}",
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
                $"state={Uri.EscapeDataString(state)}",
                $"code_challenge={Uri.EscapeDataString(codeChallenge)}",
                $"code_challenge_method=S256"
            };

            if (!string.IsNullOrEmpty(scope))
            {
                queryParams.Add($"scope={Uri.EscapeDataString(scope)}");
            }

            string separator = authUrl.Contains("?") ? "&" : "?";
            return authUrl + separator + string.Join("&", queryParams);
        }

        private async Task WaitForCallback(CancellationToken cancellationToken)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // Parse the callback URL
                string code = request.QueryString["code"];
                string state = request.QueryString["state"];
                string error = request.QueryString["error"];
                string errorDescription = request.QueryString["error_description"];

                // Verify state
                if (!string.IsNullOrEmpty(state) && state != _generatedState)
                {
                    _error = "State mismatch - possible CSRF attack";
                    SendResponse(response, "Authorization Failed", "State verification failed. Please try again.");
                }
                else if (!string.IsNullOrEmpty(error))
                {
                    _error = string.IsNullOrEmpty(errorDescription) ? error : $"{error}: {errorDescription}";
                    SendResponse(response, "Authorization Failed", $"Error: {_error}");
                }
                else if (!string.IsNullOrEmpty(code))
                {
                    _authorizationCode = code;
                    _returnedState = state;
                    SendResponse(response, "Authorization Successful", "You can close this window and return to Grasshopper.");
                }
                else
                {
                    _error = "No authorization code received";
                    SendResponse(response, "Authorization Failed", "No authorization code was received.");
                }

                _completed = true;
                _waiting = false;

                // Trigger UI update
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    ExpireSolution(true);
                });
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
                _error = ex.Message;
                _completed = true;
                _waiting = false;

                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    ExpireSolution(true);
                });
            }
        }

        private void SendResponse(HttpListenerResponse response, string title, string message)
        {
            string html = $@"<!DOCTYPE html>
<html>
<head>
    <title>{title}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
               display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;
               background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }}
        .container {{ background: white; padding: 40px; border-radius: 10px; text-align: center;
                     box-shadow: 0 10px 40px rgba(0,0,0,0.2); max-width: 400px; }}
        h1 {{ color: #333; margin-bottom: 10px; }}
        p {{ color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>{title}</h1>
        <p>{message}</p>
    </div>
</body>
</html>";

            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private void StopListener()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _listener?.Stop();
                _listener?.Close();
                _listener = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Generates a cryptographically random code verifier for PKCE.
        /// </summary>
        private string GenerateCodeVerifier()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[32];
                rng.GetBytes(bytes);
                return Base64UrlEncode(bytes);
            }
        }

        /// <summary>
        /// Generates the code challenge from the code verifier using SHA256.
        /// </summary>
        private string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.ASCII.GetBytes(codeVerifier);
                byte[] hash = sha256.ComputeHash(bytes);
                return Base64UrlEncode(hash);
            }
        }

        /// <summary>
        /// Generates a random state string for CSRF protection.
        /// </summary>
        private string GenerateState()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[16];
                rng.GetBytes(bytes);
                return Base64UrlEncode(bytes);
            }
        }

        /// <summary>
        /// Base64 URL encoding (without padding).
        /// </summary>
        private string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
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
            get { return null; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("B2C3D4E5-F6A7-4B8C-9D0E-1F2A3B4C5D6E"); }
        }
    }
}
