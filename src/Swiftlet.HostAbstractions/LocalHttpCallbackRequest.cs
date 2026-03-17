namespace Swiftlet.HostAbstractions;

public sealed class LocalHttpCallbackRequest
{
    public LocalHttpCallbackRequest(
        Uri requestUri,
        string httpMethod,
        IReadOnlyDictionary<string, string?> queryParameters)
    {
        RequestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        QueryParameters = queryParameters ?? throw new ArgumentNullException(nameof(queryParameters));
    }

    public Uri RequestUri { get; }

    public string HttpMethod { get; }

    public IReadOnlyDictionary<string, string?> QueryParameters { get; }

    public string? GetQueryParameter(string name)
    {
        return QueryParameters.TryGetValue(name, out string? value) ? value : null;
    }
}
