using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Swiftlet.Util
{
    /// <summary>
    /// Provides a shared HttpClient instance to prevent socket exhaustion.
    /// HttpClient is designed to be instantiated once and reused throughout the life of an application.
    /// Creating a new HttpClient per request can exhaust the number of sockets available under heavy loads.
    /// </summary>
    public static class HttpClientFactory
    {
        /// <summary>
        /// Shared HttpClient instance. Thread-safe for concurrent use.
        /// Note: Do not use DefaultRequestHeaders on this client; add headers to HttpRequestMessage instead.
        /// The Timeout is set to infinite because we use per-request timeouts via CancellationToken.
        /// </summary>
        public static readonly HttpClient SharedClient;

        /// <summary>
        /// Default timeout in seconds (100 seconds, matching .NET's default HttpClient timeout).
        /// </summary>
        public const int DefaultTimeoutSeconds = 100;

        static HttpClientFactory()
        {
            SharedClient = new HttpClient();
            // Set to infinite because we handle timeouts per-request with CancellationToken
            SharedClient.Timeout = TimeSpan.FromMilliseconds(-1);
        }

        /// <summary>
        /// Sends an HTTP request with a configurable timeout.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="timeoutSeconds">Timeout in seconds. Use 0 or negative for no timeout.</param>
        /// <returns>The HTTP response message.</returns>
        public static HttpResponseMessage SendWithTimeout(HttpRequestMessage request, int timeoutSeconds)
        {
            if (timeoutSeconds <= 0)
            {
                return SharedClient.SendAsync(request).Result;
            }

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                try
                {
                    return SharedClient.SendAsync(request, cts.Token).Result;
                }
                catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
                {
                    throw new TimeoutException($"Request timed out after {timeoutSeconds} seconds");
                }
                catch (TaskCanceledException)
                {
                    throw new TimeoutException($"Request timed out after {timeoutSeconds} seconds");
                }
            }
        }
    }
}
