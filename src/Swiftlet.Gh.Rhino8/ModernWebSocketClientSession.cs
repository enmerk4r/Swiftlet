using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketClientSession : IAsyncDisposable
{
    private readonly ModernWebSocketClient _client = new();

    public event EventHandler? StateChanged
    {
        add => _client.StateChanged += value;
        remove => _client.StateChanged -= value;
    }

    public ModernWebSocketConnection? Connection => _client.Connection;

    public string? LastError => _client.LastError;

    public bool IsConnected => _client.IsConnected;

    public async Task ReconnectAsync(
        string url,
        IEnumerable<QueryParameter>? parameters,
        CancellationToken cancellationToken = default)
    {
        string fullUrl = UrlBuilder.AddQueryParameters(url, parameters);
        await _client.ConnectAsync(fullUrl, cancellationToken).ConfigureAwait(false);
    }

    public bool TryDequeueMessage(out string? message)
    {
        return _client.TryDequeueMessage(out message);
    }

    public Task DisconnectAsync()
    {
        return _client.DisconnectAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
    }
}
