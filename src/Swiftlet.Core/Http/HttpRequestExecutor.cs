using System.Net.Http;

namespace Swiftlet.Core.Http;

public sealed class HttpRequestExecutor
{
    private readonly HttpClient _httpClient;

    public HttpRequestExecutor(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public HttpResponseData Execute(HttpRequestDefinition requestDefinition, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(requestDefinition, cancellationToken).GetAwaiter().GetResult();
    }

    public async Task<HttpResponseData> ExecuteAsync(HttpRequestDefinition requestDefinition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestDefinition);

        string fullUrl = requestDefinition.BuildUrl();
        using var request = new HttpRequestMessage(HttpMethodFactory.Create(requestDefinition.Method), fullUrl);

        foreach (HttpHeader header in requestDefinition.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (ShouldSendBody(request.Method) && requestDefinition.Body is not null)
        {
            request.Content = requestDefinition.Body.ToHttpContent();
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(requestDefinition.TimeoutSeconds));

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request, timeoutSource.Token);
            return HttpResponseData.FromHttpResponseMessage(response);
        }
        catch (Exception ex)
        {
            return new HttpResponseData("0.0", -1, ex.Message, [], false, string.Empty, []);
        }
    }

    private static bool ShouldSendBody(HttpMethod method)
    {
        return method == HttpMethod.Post ||
               method == HttpMethod.Put ||
               method.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
    }
}
