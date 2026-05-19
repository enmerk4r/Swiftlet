namespace Swiftlet.HostAbstractions;

public interface ILocalHttpCallbackListenerFactory
{
    ValueTask<ILocalHttpCallbackSession> StartAsync(Uri callbackUri, CancellationToken cancellationToken = default);
}
