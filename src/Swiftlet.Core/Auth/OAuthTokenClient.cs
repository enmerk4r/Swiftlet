using System.Net.Http;

namespace Swiftlet.Core.Auth;

public sealed class OAuthTokenClient
{
    private readonly HttpClient _httpClient;

    public OAuthTokenClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public OAuthTokenResponse Exchange(OAuthTokenRequest request, CancellationToken cancellationToken = default)
    {
        return ExchangeAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    public async Task<OAuthTokenResponse> ExchangeAsync(OAuthTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.TokenUrl)
        {
            Content = request.ToRequestBody().ToHttpContent(),
        };

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string reasonPhrase = string.IsNullOrWhiteSpace(response.ReasonPhrase)
                ? "Error"
                : response.ReasonPhrase;
            string fallbackMessage = $"The remote server returned an error: ({(int)response.StatusCode}) {reasonPhrase}.";
            throw OAuthTokenException.FromJson(responseBody, fallbackMessage);
        }

        return OAuthTokenResponse.FromJson(responseBody);
    }
}
