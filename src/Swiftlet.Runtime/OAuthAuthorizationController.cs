using Swiftlet.HostAbstractions;

namespace Swiftlet.Runtime;

public sealed record OAuthAuthorizationSnapshot(string? Code = null, string? Verifier = null,
    string? RedirectUri = null, string Status = "Ready (set Authorize to true to start)",
    string? Error = null, string? Warning = null, bool IsWaiting = false);

/// <summary>One owned authorization attempt at a time; callbacks from superseded attempts cannot publish.</summary>
public sealed class OAuthAuthorizationController : IAsyncDisposable
{
    private readonly IHostServices _host;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ModernOAuthAuthorizationFlow? _flow;
    private CancellationTokenSource? _waiting;
    private OAuthAuthorizationSnapshot _snapshot = new();
    private long _generation;
    private bool _previousAuthorize;
    public event EventHandler? Changed;
    public OAuthAuthorizationSnapshot Snapshot => Volatile.Read(ref _snapshot);
    public OAuthAuthorizationController(IHostServices host) => _host = host;

    public async Task<OAuthAuthorizationSnapshot> EvaluateAsync(string url, string clientId,
        IReadOnlyList<string> scopes, int port, bool authorize, CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            bool triggered = authorize && !_previousAuthorize; _previousAuthorize = authorize;
            string redirect = $"http://localhost:{port}/callback/";
            if (string.IsNullOrWhiteSpace(url)) return new(Status: "Error: No authorization URL", Error: "Authorization URL is required");
            if (string.IsNullOrWhiteSpace(clientId)) return new(Status: "Error: No client ID", Error: "Client ID is required");
            if (port < 1 || port > 65535) return new(Status: "Error: Invalid port", Error: "Port must be between 1 and 65535");
            if (triggered)
            {
                await StopUnderGateAsync().ConfigureAwait(false);
                var flow = new ModernOAuthAuthorizationFlow(_host); _flow = flow;
                var waiting = new CancellationTokenSource(); _waiting = waiting;
                long generation = ++_generation;
                try
                {
                    var launch = await flow.StartAsync(url, clientId, redirect, scopes, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    string? error = !launch.IsSuccess && !launch.RequiresManualAction ? launch.Message : null;
                    string? warning = launch.RequiresManualAction ? string.IsNullOrWhiteSpace(launch.ManualActionText)
                        ? launch.Message : $"{launch.Message} {launch.ManualActionText}" : null;
                    _snapshot = new(Verifier: flow.Session?.CodeVerifier, RedirectUri: flow.Session?.RedirectUri ?? redirect,
                        Status: error is not null ? $"Error: {error}" : warning ?? "Waiting for authorization... (check your browser)",
                        Error: error, Warning: warning, IsWaiting: error is null);
                    if (error is null) _ = CompleteAsync(flow, generation, waiting.Token);
                    else await StopUnderGateAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                { await StopUnderGateAsync().ConfigureAwait(false); _previousAuthorize = false; throw; }
                catch (Exception error)
                {
                    await StopUnderGateAsync().ConfigureAwait(false);
                    _snapshot = new(Status: $"Error: {error.Message}", Error: error.Message, RedirectUri: redirect);
                }
            }
            return _snapshot with { RedirectUri = _snapshot.RedirectUri ?? redirect };
        }
        finally { _gate.Release(); }
    }
    private async Task CompleteAsync(ModernOAuthAuthorizationFlow flow, long generation, CancellationToken token)
    {
        string? code = null, error = null;
        try
        {
            var result = await flow.WaitForCompletionAsync(token).ConfigureAwait(false);
            code = result.AuthorizationCode; error = result.Error;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
        catch (Exception failure) { error = failure.Message; }
        await _gate.WaitAsync().ConfigureAwait(false);
        bool publish = false;
        try
        {
            if (generation != _generation || !ReferenceEquals(flow, _flow) || token.IsCancellationRequested) return;
            _snapshot = _snapshot with { Code = code, Error = error, Warning = null, IsWaiting = false,
                Status = !string.IsNullOrWhiteSpace(code) ? "Authorization successful" : $"Error: {error}" };
            publish = true;
        }
        finally { _gate.Release(); }
        if (publish) Changed?.Invoke(this, EventArgs.Empty);
    }
    public async Task ResetAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { await StopUnderGateAsync().ConfigureAwait(false); _snapshot = new(); _previousAuthorize = false; }
        finally { _gate.Release(); }
    }
    private async Task StopUnderGateAsync()
    {
        ++_generation;
        var waiting = _waiting; _waiting = null; waiting?.Cancel();
        var flow = _flow; _flow = null;
        if (flow is not null) await flow.DisposeAsync().ConfigureAwait(false);
        waiting?.Dispose();
    }
    public async ValueTask DisposeAsync() => await ResetAsync().ConfigureAwait(false);
}
