using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    /// <summary>
    /// OAuth 2.0 Token Exchange component that exchanges an authorization code for access and refresh tokens.
    /// Also supports refreshing tokens using a refresh token.
    /// Supports PKCE for enhanced security.
    /// </summary>
    public class OAuthTokenComponent : GH_Component
    {
        // Cache last successful token response
        private string _cachedAccessToken;
        private string _cachedRefreshToken;
        private int _cachedExpiresIn;
        private string _cachedTokenType;

        public OAuthTokenComponent()
          : base("OAuth Token", "Token",
              "Exchanges an OAuth 2.0 authorization code for access and refresh tokens.\nSet Refresh to true to use a refresh token to obtain new tokens.",
              NamingUtility.CATEGORY, NamingUtility.AUTH)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token URL", "U", "Token endpoint URL (e.g., https://oauth2.googleapis.com/token)", GH_ParamAccess.item);
            pManager.AddTextParameter("Client ID", "ID", "OAuth Client ID", GH_ParamAccess.item);
            pManager.AddTextParameter("Code", "C", "Authorization code from OAuth Authorize component", GH_ParamAccess.item);
            pManager.AddTextParameter("Code Verifier", "V", "PKCE code verifier from OAuth Authorize component", GH_ParamAccess.item);
            pManager.AddTextParameter("Redirect URI", "R", "Redirect URI (must match the one used in authorization)", GH_ParamAccess.item);
            pManager.AddTextParameter("Client Secret", "S", "Client secret (optional - not needed for PKCE public clients)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Refresh", "Rf", "Set to true to refresh the token using the Refresh Token output. Use momentarily (e.g., with a Button), not persistently.", GH_ParamAccess.item, false);

            pManager[3].Optional = true; // Code verifier optional for non-PKCE flows
            pManager[5].Optional = true; // Client secret optional for public clients
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Access Token", "AT", "Access token for API requests", GH_ParamAccess.item);
            pManager.AddTextParameter("Refresh Token", "RT", "Refresh token for obtaining new access tokens", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Expires In", "E", "Token expiration time in seconds", GH_ParamAccess.item);
            pManager.AddTextParameter("Token Type", "T", "Token type (usually 'Bearer')", GH_ParamAccess.item);
            pManager.AddParameter(new HttpHeaderParam(), "Auth Header", "H", "Ready-to-use Authorization header", GH_ParamAccess.item);
            pManager.AddTextParameter("Error", "Err", "Error message if token exchange failed", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string tokenUrl = string.Empty;
            string clientId = string.Empty;
            string code = string.Empty;
            string codeVerifier = string.Empty;
            string redirectUri = string.Empty;
            string clientSecret = string.Empty;
            bool refresh = false;

            DA.GetData(0, ref tokenUrl);
            DA.GetData(1, ref clientId);
            DA.GetData(2, ref code);
            DA.GetData(3, ref codeVerifier);
            DA.GetData(4, ref redirectUri);
            DA.GetData(5, ref clientSecret);
            DA.GetData(6, ref refresh);

            // Warn user if Refresh is left on
            if (refresh)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Refresh is enabled. Set back to False after refreshing to avoid repeated token requests.");
            }

            // Validate required inputs
            if (string.IsNullOrEmpty(tokenUrl))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Token URL is required");
                DA.SetData(5, "Token URL is required");
                return;
            }

            if (string.IsNullOrEmpty(clientId))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Client ID is required");
                DA.SetData(5, "Client ID is required");
                return;
            }

            // Handle refresh token flow
            if (refresh)
            {
                if (string.IsNullOrEmpty(_cachedRefreshToken))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No refresh token available. Obtain tokens first with Refresh set to False.");
                    DA.SetData(5, "No refresh token available");
                    return;
                }

                PerformTokenRequest(DA, tokenUrl, clientId, clientSecret, isRefresh: true);
                return;
            }

            // Authorization code flow - output cached values if we have them and no new code
            if (string.IsNullOrEmpty(code))
            {
                if (!string.IsNullOrEmpty(_cachedAccessToken))
                {
                    OutputCachedTokens(DA);
                    return;
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Authorization code is required");
                DA.SetData(5, "Waiting for authorization code");
                return;
            }

            if (string.IsNullOrEmpty(redirectUri))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Redirect URI is required");
                DA.SetData(5, "Redirect URI is required");
                return;
            }

            PerformTokenRequest(DA, tokenUrl, clientId, clientSecret, isRefresh: false, code, codeVerifier, redirectUri);
        }

        private void PerformTokenRequest(IGH_DataAccess DA, string tokenUrl, string clientId, string clientSecret, bool isRefresh, string code = null, string codeVerifier = null, string redirectUri = null)
        {
            try
            {
                var parameters = new List<string>
                {
                    $"client_id={Uri.EscapeDataString(clientId)}"
                };

                if (isRefresh)
                {
                    parameters.Add("grant_type=refresh_token");
                    parameters.Add($"refresh_token={Uri.EscapeDataString(_cachedRefreshToken)}");
                }
                else
                {
                    parameters.Add("grant_type=authorization_code");
                    parameters.Add($"code={Uri.EscapeDataString(code)}");
                    parameters.Add($"redirect_uri={Uri.EscapeDataString(redirectUri)}");

                    // Add PKCE code verifier if provided
                    if (!string.IsNullOrEmpty(codeVerifier))
                    {
                        parameters.Add($"code_verifier={Uri.EscapeDataString(codeVerifier)}");
                    }
                }

                // Add client secret if provided
                if (!string.IsNullOrEmpty(clientSecret))
                {
                    parameters.Add($"client_secret={Uri.EscapeDataString(clientSecret)}");
                }

                string requestBody = string.Join("&", parameters);

                // Make the token request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tokenUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bodyBytes.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bodyBytes, 0, bodyBytes.Length);
                }

                // Parse the response
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string responseBody = reader.ReadToEnd();
                    JObject json = JObject.Parse(responseBody);

                    _cachedAccessToken = json["access_token"]?.ToString();
                    // Some providers rotate refresh tokens, others keep the same one
                    string newRefreshToken = json["refresh_token"]?.ToString();
                    if (!string.IsNullOrEmpty(newRefreshToken))
                    {
                        _cachedRefreshToken = newRefreshToken;
                    }
                    _cachedExpiresIn = json["expires_in"]?.Value<int>() ?? 0;
                    _cachedTokenType = json["token_type"]?.ToString() ?? "Bearer";

                    OutputCachedTokens(DA);
                    this.Message = isRefresh ? "Refreshed" : "Token received";
                }
            }
            catch (WebException ex)
            {
                string errorMessage = isRefresh ? "Token refresh failed" : "Token exchange failed";

                if (ex.Response != null)
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            string errorBody = reader.ReadToEnd();
                            JObject errorJson = JObject.Parse(errorBody);

                            string error = errorJson["error"]?.ToString();
                            string errorDescription = errorJson["error_description"]?.ToString();

                            errorMessage = string.IsNullOrEmpty(errorDescription)
                                ? error ?? errorMessage
                                : $"{error}: {errorDescription}";
                        }
                    }
                    catch
                    {
                        errorMessage = ex.Message;
                    }
                }
                else
                {
                    errorMessage = ex.Message;
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorMessage);
                DA.SetData(5, errorMessage);
                this.Message = "Error";
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                DA.SetData(5, ex.Message);
                this.Message = "Error";
            }
        }

        private void OutputCachedTokens(IGH_DataAccess DA)
        {
            DA.SetData(0, _cachedAccessToken);
            DA.SetData(1, _cachedRefreshToken);
            DA.SetData(2, _cachedExpiresIn);
            DA.SetData(3, _cachedTokenType);

            if (!string.IsNullOrEmpty(_cachedAccessToken))
            {
                DA.SetData(4, new HttpHeaderGoo("Authorization", $"{_cachedTokenType} {_cachedAccessToken}"));
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.Icons_oauth_token; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("C3D4E5F6-A7B8-4C9D-0E1F-2A3B4C5D6E7F"); }
        }
    }
}
