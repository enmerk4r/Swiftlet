using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Headless;

public sealed class UnsupportedLocalHttpCallbackListenerFactory : ILocalHttpCallbackListenerFactory
{
    public ValueTask<ILocalHttpCallbackSession> StartAsync(Uri callbackUri, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"Local HTTP callbacks are not available for '{callbackUri}'.");
    }
}
