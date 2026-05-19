using System.Net.Http;

namespace Swiftlet.Core.Http;

public static class HttpMethodFactory
{
    public static HttpMethod Create(string method)
    {
        Guard.ThrowIfNullOrWhiteSpace(method, nameof(method));

        return method.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "PATCH" => new HttpMethod("PATCH"),
            "HEAD" => HttpMethod.Head,
            "TRACE" => HttpMethod.Trace,
            "CONNECT" => new HttpMethod("CONNECT"),
            "OPTIONS" => HttpMethod.Options,
            _ => new HttpMethod(method.ToUpperInvariant()),
        };
    }
}
