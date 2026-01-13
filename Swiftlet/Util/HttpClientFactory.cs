using System.Net.Http;

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
        /// </summary>
        public static readonly HttpClient SharedClient = new HttpClient();
    }
}
