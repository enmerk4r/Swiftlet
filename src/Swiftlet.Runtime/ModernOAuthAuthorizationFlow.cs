using Swiftlet.Core.Auth;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Runtime;

public sealed class ModernOAuthAuthorizationFlow : IAsyncDisposable
{
    private readonly IHostServices _hostServices;
    private ILocalHttpCallbackSession? _callbackSession;

    public ModernOAuthAuthorizationFlow(IHostServices hostServices)
    {
        _hostServices = hostServices ?? throw new ArgumentNullException(nameof(hostServices));
    }

    public OAuthAuthorizationSession? Session { get; private set; }

    public string? AuthorizationCode { get; private set; }

    public string? ReturnedState { get; private set; }

    public string? Error { get; private set; }

    public bool IsWaiting { get; private set; }

    public bool IsCompleted { get; private set; }

    public string StatusMessage =>
        IsCompleted && !string.IsNullOrWhiteSpace(AuthorizationCode)
            ? "Authorization successful"
            : IsCompleted && !string.IsNullOrWhiteSpace(Error)
                ? $"Error: {Error}"
                : IsWaiting
                    ? "Waiting for authorization... (check your browser)"
                    : "Ready (start authorization to continue)";

    public async Task<HostActionResult> StartAsync(
        string authorizationUrl,
        string clientId,
        string redirectUri,
        IEnumerable<string>? scopes,
        CancellationToken cancellationToken = default)
    {
        await ResetAsync().ConfigureAwait(false);

        Session = ModernOAuthWorkflow.CreateAuthorizationSession(
            authorizationUrl,
            clientId,
            redirectUri,
            scopes);

        Uri callbackUri = new(Session.RedirectUri, UriKind.Absolute);
        _callbackSession = await _hostServices.LocalCallbacks
            .StartAsync(callbackUri, cancellationToken)
            .ConfigureAwait(false);

        HostActionResult launchResult = await ModernOAuthWorkflow
            .LaunchAuthorizationUrlAsync(_hostServices, Session, cancellationToken)
            .ConfigureAwait(false);

        if (launchResult.IsSuccess || launchResult.RequiresManualAction)
        {
            IsWaiting = true;
            IsCompleted = false;
            Error = null;
        }
        else
        {
            Error = launchResult.Message;
            IsWaiting = false;
            IsCompleted = true;
        }

        return launchResult;
    }

    public async Task<OAuthCallbackResult> WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        var callback = _callbackSession;
        var session = Session;
        if (callback is null || session is null)
        {
            throw new InvalidOperationException("Authorization flow has not been started.");
        }

        OAuthCallbackResult result = await ModernOAuthWorkflow
            .WaitForAuthorizationCodeAsync(callback, session, cancellationToken)
            .ConfigureAwait(false);

        AuthorizationCode = result.AuthorizationCode;
        ReturnedState = result.ReturnedState;
        Error = result.Error;
        IsWaiting = false;
        IsCompleted = true;

        await callback.DisposeAsync().ConfigureAwait(false);
        Interlocked.CompareExchange(ref _callbackSession, null, callback);

        return result;
    }

    public async Task ResetAsync()
    {
        var callback = Volatile.Read(ref _callbackSession);
        if (callback is not null)
        {
            await callback.DisposeAsync().ConfigureAwait(false);
            Interlocked.CompareExchange(ref _callbackSession, null, callback);
        }

        Session = null;
        AuthorizationCode = null;
        ReturnedState = null;
        Error = null;
        IsWaiting = false;
        IsCompleted = false;
    }

    public async ValueTask DisposeAsync()
    {
        await ResetAsync().ConfigureAwait(false);
    }
}
