using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

public sealed class SimpleHttpListenerRequest
{
    public SimpleHttpListenerRequest(
        string path,
        string method,
        IEnumerable<HttpHeader>? headers,
        IEnumerable<QueryParameter>? queryParameters,
        string? content)
    {
        Path = string.IsNullOrWhiteSpace(path) ? "/" : path;
        Method = string.IsNullOrWhiteSpace(method) ? "GET" : method;
        Headers = headers?.Select(static header => new HttpHeader(header.Key, header.Value)).ToArray() ?? [];
        QueryParameters = queryParameters?.Select(static parameter => new QueryParameter(parameter.Key, parameter.Value)).ToArray() ?? [];
        Content = content ?? string.Empty;
    }

    public string Path { get; }

    public string Method { get; }

    public IReadOnlyList<HttpHeader> Headers { get; }

    public IReadOnlyList<QueryParameter> QueryParameters { get; }

    public string Content { get; }
}
