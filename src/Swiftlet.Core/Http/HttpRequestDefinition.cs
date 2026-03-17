namespace Swiftlet.Core.Http;

public sealed class HttpRequestDefinition
{
    public HttpRequestDefinition(
        string url,
        string method,
        IRequestBody? body = null,
        IEnumerable<QueryParameter>? queryParameters = null,
        IEnumerable<HttpHeader>? headers = null,
        int timeoutSeconds = 100)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Body = body;
        QueryParameters = queryParameters?.Select(static parameter => new QueryParameter(parameter.Key, parameter.Value)).ToList() ?? [];
        Headers = headers?.Select(static header => new HttpHeader(header.Key, header.Value)).ToList() ?? [];
        TimeoutSeconds = timeoutSeconds;
    }

    public string Url { get; }

    public string Method { get; }

    public IRequestBody? Body { get; }

    public List<QueryParameter> QueryParameters { get; }

    public List<HttpHeader> Headers { get; }

    public int TimeoutSeconds { get; }

    public string BuildUrl()
    {
        return UrlBuilder.AddQueryParameters(Url, QueryParameters);
    }
}
