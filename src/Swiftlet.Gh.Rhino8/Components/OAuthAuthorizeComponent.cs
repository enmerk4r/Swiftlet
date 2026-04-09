using Grasshopper.Kernel;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class OAuthAuthorizeComponent : GH_Component
{
    private readonly RhinoHostServices _hostServices = new();
    private readonly ModernOAuthAuthorizationFlow _flow;
    private int _runId;
    private int _updateScheduled;
    private bool _previousAuthorize;
    private string? _launchMessage;
    private string? _componentError;

    public OAuthAuthorizeComponent()
        : base(
            "OAuth Authorize",
            "OAuth",
            "Initiates OAuth 2.0 Authorization Code flow with PKCE.\nOpens browser for user authentication and receives the authorization code via localhost callback.",
            ShellNaming.Category,
            ShellNaming.Auth)
    {
        _flow = new ModernOAuthAuthorizationFlow(_hostServices);
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
        string authorizationUrl = string.Empty;
        string clientId = string.Empty;
        List<string> scopes = [];
        int port = 8888;
        bool authorize = false;

        if (!DA.GetData(0, ref authorizationUrl) || !DA.GetData(1, ref clientId))
        {
            return;
        }

        DA.GetDataList(2, scopes);
        DA.GetData(3, ref port);
        DA.GetData(4, ref authorize);
        bool authorizeTriggered = authorize && !_previousAuthorize;
        _previousAuthorize = authorize;

        string redirectUri = $"http://localhost:{port}/callback/";

        if (string.IsNullOrWhiteSpace(authorizationUrl))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Authorization URL is required");
            DA.SetData(3, "Error: No authorization URL");
            return;
        }

        if (string.IsNullOrWhiteSpace(clientId))
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

        if (authorizeTriggered)
        {
            ResetFlow();
            StartAuthorizationFlow(authorizationUrl, clientId, redirectUri, scopes);
        }

        string status = GetStatusMessage();

        Message = _flow.IsCompleted && !string.IsNullOrWhiteSpace(_flow.AuthorizationCode)
            ? "Authorized"
            : _flow.IsCompleted && !string.IsNullOrWhiteSpace(_flow.Error)
                ? "Error"
                : !string.IsNullOrWhiteSpace(_componentError)
                    ? "Error"
                : _flow.IsWaiting
                    ? "Waiting..."
                    : string.Empty;

        DA.SetData(0, _flow.AuthorizationCode);
        DA.SetData(1, _flow.Session?.CodeVerifier);
        DA.SetData(2, _flow.Session?.RedirectUri ?? redirectUri);
        DA.SetData(3, status);
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        ResetFlow();
        _flow.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            ResetFlow();
        }

        base.DocumentContextChanged(document, context);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B2C3D4E5-F6A7-4B8C-9D0E-1F2A3B4C5D6E");

    private void StartAuthorizationFlow(
        string authorizationUrl,
        string clientId,
        string redirectUri,
        IReadOnlyList<string> scopes)
    {
        try
        {
            _componentError = null;
            _launchMessage = null;

            HostActionResult launchResult = _flow
                .StartAsync(authorizationUrl, clientId, redirectUri, scopes)
                .GetAwaiter()
                .GetResult();

            if (!launchResult.IsSuccess && !launchResult.RequiresManualAction)
            {
                _componentError = launchResult.Message;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, launchResult.Message);
                return;
            }

            if (launchResult.RequiresManualAction)
            {
                _launchMessage = string.IsNullOrWhiteSpace(launchResult.ManualActionText)
                    ? launchResult.Message
                    : $"{launchResult.Message} {launchResult.ManualActionText}";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, _launchMessage);
            }

            int runId = ++_runId;
            _ = WaitForAuthorizationAsync(runId);

            ScheduleComponentUpdate();
        }
        catch (Exception ex)
        {
            _componentError = ex.Message;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    private async Task WaitForAuthorizationAsync(int runId)
    {
        try
        {
            await _flow.WaitForCompletionAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (runId == _runId)
            {
                _componentError = ex.Message;
            }
        }
        finally
        {
            ScheduleComponentUpdate();
        }
    }

    private string GetStatusMessage()
    {
        if (_flow.IsCompleted && !string.IsNullOrWhiteSpace(_flow.AuthorizationCode))
        {
            return "Authorization successful";
        }

        if (_flow.IsCompleted && !string.IsNullOrWhiteSpace(_flow.Error))
        {
            return $"Error: {_flow.Error}";
        }

        if (!string.IsNullOrWhiteSpace(_componentError))
        {
            return $"Error: {_componentError}";
        }

        if (!string.IsNullOrWhiteSpace(_launchMessage))
        {
            return _launchMessage;
        }

        if (_flow.IsWaiting)
        {
            return "Waiting for authorization... (check your browser)";
        }

        return "Ready (set Authorize to true to start)";
    }

    private void ResetFlow()
    {
        _runId++;
        _flow.ResetAsync().GetAwaiter().GetResult();
        _launchMessage = null;
        _componentError = null;
        Message = string.Empty;
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
}

