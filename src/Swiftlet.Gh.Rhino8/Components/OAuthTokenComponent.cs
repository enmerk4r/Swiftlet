using Grasshopper.Kernel;
using Swiftlet.Core.Auth;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class OAuthTokenComponent : GH_Component
{
    private readonly OAuthTokenClient _tokenClient = new();
    private string _cachedAccessToken = string.Empty;
    private string _cachedRefreshToken = string.Empty;
    private int _cachedExpiresIn;
    private string _cachedTokenType = "Bearer";

    public OAuthTokenComponent()
        : base(
            "OAuth Token",
            "Token",
            "Exchanges an OAuth 2.0 authorization code for access and refresh tokens.\nSet Refresh to true to use a refresh token to obtain new tokens.",
            ShellNaming.Category,
            ShellNaming.Auth)
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

        pManager[3].Optional = true;
        pManager[5].Optional = true;
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

        if (refresh)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Refresh is enabled. Set back to False after refreshing to avoid repeated token requests.");
        }

        if (string.IsNullOrWhiteSpace(tokenUrl))
        {
            SetValidationError(DA, "Token URL is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            SetValidationError(DA, "Client ID is required");
            return;
        }

        if (refresh)
        {
            if (string.IsNullOrWhiteSpace(_cachedRefreshToken))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No refresh token available. Obtain tokens first with Refresh set to False.");
                DA.SetData(5, "No refresh token available");
                return;
            }

            ExchangeToken(DA, ModernOAuthWorkflow.CreateRefreshTokenRequest(tokenUrl, clientId, _cachedRefreshToken, clientSecret), true);
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken))
            {
                OutputCachedTokens(DA);
                return;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Authorization code is required");
            DA.SetData(5, "Waiting for authorization code");
            return;
        }

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            SetValidationError(DA, "Redirect URI is required");
            return;
        }

        OAuthTokenRequest request = ModernOAuthWorkflow.CreateAuthorizationCodeTokenRequest(
            tokenUrl,
            clientId,
            code,
            redirectUri,
            string.IsNullOrWhiteSpace(codeVerifier) ? null : codeVerifier,
            string.IsNullOrWhiteSpace(clientSecret) ? null : clientSecret);

        ExchangeToken(DA, request, false);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("C3D4E5F6-A7B8-4C9D-0E1F-2A3B4C5D6E7F");

    private void ExchangeToken(IGH_DataAccess DA, OAuthTokenRequest request, bool isRefresh)
    {
        try
        {
            OAuthTokenResponse response = _tokenClient.Exchange(request);
            _cachedAccessToken = response.AccessToken;
            if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                _cachedRefreshToken = response.RefreshToken;
            }

            _cachedExpiresIn = response.ExpiresIn;
            _cachedTokenType = string.IsNullOrWhiteSpace(response.TokenType) ? "Bearer" : response.TokenType;

            OutputCachedTokens(DA);
            Message = isRefresh ? "Refreshed" : "Token received";
            DA.SetData(5, null);
        }
        catch (OAuthTokenException ex)
        {
            SetError(DA, ex.Message);
        }
        catch (Exception ex)
        {
            SetError(DA, ex.Message);
        }
    }

    private void OutputCachedTokens(IGH_DataAccess DA)
    {
        DA.SetData(0, _cachedAccessToken);
        DA.SetData(1, _cachedRefreshToken);
        DA.SetData(2, _cachedExpiresIn);
        DA.SetData(3, _cachedTokenType);

        if (!string.IsNullOrWhiteSpace(_cachedAccessToken))
        {
            DA.SetData(4, new HttpHeaderGoo("Authorization", $"{_cachedTokenType} {_cachedAccessToken}"));
        }

        DA.SetData(5, null);
    }

    private void SetError(IGH_DataAccess dataAccess, string message)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
        Message = "Error";
        dataAccess.SetData(5, message);
    }

    private void SetValidationError(IGH_DataAccess dataAccess, string message)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
        dataAccess.SetData(5, message);
    }
}

