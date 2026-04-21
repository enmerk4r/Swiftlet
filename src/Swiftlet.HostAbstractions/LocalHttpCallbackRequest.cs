namespace Swiftlet.HostAbstractions;

public sealed class LocalHttpCallbackRequest
{
    public LocalHttpCallbackRequest(
        Uri requestUri,
        string httpMethod,
        IReadOnlyDictionary<string, string?> queryParameters,
        IReadOnlyDictionary<string, string> headers,
        string rawBody)
    {
        RequestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        QueryParameters = queryParameters ?? throw new ArgumentNullException(nameof(queryParameters));
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        RawBody = rawBody ?? string.Empty;
    }

    public Uri RequestUri { get; }

    public string HttpMethod { get; }

    public IReadOnlyDictionary<string, string?> QueryParameters { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string RawBody { get; }

    public string? GetQueryParameter(string name)
    {
        return QueryParameters.TryGetValue(name, out string? value) ? value : null;
    }

    public string? GetHeader(string name)
    {
        return Headers.TryGetValue(name, out string? value) ? value : null;
    }
}
