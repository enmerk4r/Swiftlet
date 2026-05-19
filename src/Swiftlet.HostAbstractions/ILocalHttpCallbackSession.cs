namespace Swiftlet.HostAbstractions;

public interface ILocalHttpCallbackSession : IAsyncDisposable
{
    Uri CallbackUri { get; }

    Task<LocalHttpCallbackRequest> WaitForCallbackAsync(CancellationToken cancellationToken = default);

    Task SendResponseAsync(LocalHttpCallbackResponse response, CancellationToken cancellationToken = default);
}
