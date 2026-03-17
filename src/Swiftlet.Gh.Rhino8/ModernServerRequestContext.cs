using System.Collections.ObjectModel;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernServerRequestContext
{
    private readonly TaskCompletionSource<ModernServerHttpResponse> _responseSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ModernServerRequestContext(
        string matchedRoute,
        string path,
        string method,
        IEnumerable<HttpHeader>? headers,
        IEnumerable<QueryParameter>? queryParameters,
        IRequestBody body)
    {
        MatchedRoute = ModernServerRouteMatcher.NormalizeRoute(matchedRoute);
        Path = string.IsNullOrWhiteSpace(path) ? "/" : path;
        Method = string.IsNullOrWhiteSpace(method) ? "GET" : method;
        Headers = new ReadOnlyCollection<HttpHeader>(
            headers?.Select(static header => new HttpHeader(header.Key, header.Value)).ToArray() ?? []);
        QueryParameters = new ReadOnlyCollection<QueryParameter>(
            queryParameters?.Select(static parameter => new QueryParameter(parameter.Key, parameter.Value)).ToArray() ?? []);
        Body = body?.Duplicate() ?? new RequestBodyBytes(ContentTypes.ApplicationOctetStream, []);
        ReceivedAtUtc = DateTime.UtcNow;
    }

    public string MatchedRoute { get; }

    public string Path { get; }

    public string Method { get; }

    public IReadOnlyList<HttpHeader> Headers { get; }

    public IReadOnlyList<QueryParameter> QueryParameters { get; }

    public IRequestBody Body { get; }

    public DateTime ReceivedAtUtc { get; }

    public bool HasResponded => ResponseTask.IsCompleted;

    public Task<ModernServerHttpResponse> ResponseTask => _responseSource.Task;

    public bool TrySetResponse(ModernServerHttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return _responseSource.TrySetResult(response);
    }
}
