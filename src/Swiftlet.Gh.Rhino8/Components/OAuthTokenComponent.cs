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
    private string? _lastCompletedRequestKey;
    private string? _lastFailedRequestKey;
    private string? _lastError;
    private bool _isExchanging;
    private bool _isRefreshExchange;
    private bool _previousRefresh;
    private int _updateScheduled;

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

        bool refreshTriggered = refresh && !_previousRefresh;
        _previousRefresh = refresh;

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
                Message = "Error";
                DA.SetData(5, "No refresh token available");
                return;
            }

            string refreshRequestKey = BuildRefreshRequestKey(tokenUrl, clientId, clientSecret, _cachedRefreshToken);

            if (_isExchanging)
            {
                OutputPending(DA, includeCachedTokens: true, _isRefreshExchange ? "Refreshing token..." : "Waiting for token request to finish...");
                return;
            }

            if (!refreshTriggered)
            {
                OutputCachedTokens(DA);
                Message = "Ready";
                return;
            }

            StartExchange(
                ModernOAuthWorkflow.CreateRefreshTokenRequest(tokenUrl, clientId, _cachedRefreshToken, clientSecret),
                refreshRequestKey,
                isRefresh: true);
            OutputPending(DA, includeCachedTokens: true, "Refreshing token...");
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken))
            {
                OutputCachedTokens(DA);
                Message = "Ready";
                return;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Authorization code is required");
            Message = "Waiting";
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

        string requestKey = BuildAuthorizationCodeRequestKey(tokenUrl, clientId, code, codeVerifier, redirectUri, clientSecret);

        if (_isExchanging)
        {
            OutputPending(DA, includeCachedTokens: false, _isRefreshExchange ? "Waiting for refresh to finish..." : "Requesting token...");
            return;
        }

        if (string.Equals(_lastCompletedRequestKey, requestKey, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(_cachedAccessToken))
        {
            OutputCachedTokens(DA);
            Message = "Token received";
            return;
        }

        if (string.Equals(_lastFailedRequestKey, requestKey, StringComparison.Ordinal))
        {
            Message = "Error";
            DA.SetData(5, _lastError);
            return;
        }

        StartExchange(request, requestKey, isRefresh: false);
        OutputPending(DA, includeCachedTokens: false, "Requesting token...");
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("C3D4E5F6-A7B8-4C9D-0E1F-2A3B4C5D6E7F");

    private void StartExchange(OAuthTokenRequest request, string requestKey, bool isRefresh)
    {
        _isExchanging = true;
        _isRefreshExchange = isRefresh;
        _lastError = null;
        _ = Task.Run(() => ExchangeTokenAsync(request, requestKey, isRefresh));
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

    private void OutputPending(IGH_DataAccess dataAccess, bool includeCachedTokens, string status)
    {
        if (includeCachedTokens)
        {
            OutputCachedTokens(dataAccess);
        }
        else
        {
            dataAccess.SetData(0, null);
            dataAccess.SetData(1, null);
            dataAccess.SetData(2, null);
            dataAccess.SetData(3, null);
            dataAccess.SetData(4, null);
            dataAccess.SetData(5, status);
        }

        Message = _isRefreshExchange ? "Refreshing..." : "Requesting...";
    }

    private async Task ExchangeTokenAsync(OAuthTokenRequest request, string requestKey, bool isRefresh)
    {
        try
        {
            OAuthTokenResponse response = await _tokenClient.ExchangeAsync(request).ConfigureAwait(false);
            _cachedAccessToken = response.AccessToken;
            if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                _cachedRefreshToken = response.RefreshToken;
            }

            _cachedExpiresIn = response.ExpiresIn;
            _cachedTokenType = string.IsNullOrWhiteSpace(response.TokenType) ? "Bearer" : response.TokenType;
            _lastCompletedRequestKey = requestKey;
            _lastFailedRequestKey = null;
            _lastError = null;
        }
        catch (OAuthTokenException ex)
        {
            _lastFailedRequestKey = requestKey;
            _lastError = ex.Message;
        }
        catch (Exception ex)
        {
            _lastFailedRequestKey = requestKey;
            _lastError = ex.Message;
        }
        finally
        {
            _isExchanging = false;
            _isRefreshExchange = false;
            ScheduleComponentUpdate();
        }
    }

    private void SetValidationError(IGH_DataAccess dataAccess, string message)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
        dataAccess.SetData(5, message);
    }

    private void ScheduleComponentUpdate()
    {
        if (Interlocked.Exchange(ref _updateScheduled, 1) == 1)
        {
            return;
        }

        Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            GH_Document? document = OnPingDocument();
            if (document is null)
            {
                Interlocked.Exchange(ref _updateScheduled, 0);
                return;
            }

            document.ScheduleSolution(5, _ =>
            {
                Interlocked.Exchange(ref _updateScheduled, 0);
                ExpireSolution(false);
            });
        }));
    }

    private static string BuildAuthorizationCodeRequestKey(
        string tokenUrl,
        string clientId,
        string code,
        string codeVerifier,
        string redirectUri,
        string clientSecret)
    {
        return string.Join("\n",
            "authorization_code",
            tokenUrl.Trim(),
            clientId.Trim(),
            code.Trim(),
            codeVerifier.Trim(),
            redirectUri.Trim(),
            clientSecret.Trim());
    }

    private static string BuildRefreshRequestKey(
        string tokenUrl,
        string clientId,
        string clientSecret,
        string refreshToken)
    {
        return string.Join("\n",
            "refresh_token",
            tokenUrl.Trim(),
            clientId.Trim(),
            clientSecret.Trim(),
            refreshToken.Trim());
    }
}

