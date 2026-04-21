using System.Net.Http;

namespace Swiftlet.Core.Http;

public sealed class HttpResponseData
{
    public HttpResponseData(
        string version,
        int statusCode,
        string? reasonPhrase,
        IEnumerable<HttpHeader>? headers,
        bool isSuccessStatusCode,
        string content,
        byte[] bytes)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase ?? string.Empty;
        Headers = headers?.Select(static header => new HttpHeader(header.Key, header.Value)).ToList() ?? [];
        IsSuccessStatusCode = isSuccessStatusCode;
        Content = content ?? string.Empty;
        Bytes = bytes?.ToArray() ?? [];
    }

    public string Version { get; }

    public int StatusCode { get; }

    public string ReasonPhrase { get; }

    public List<HttpHeader> Headers { get; }

    public bool IsSuccessStatusCode { get; }

    public string Content { get; }

    public byte[] Bytes { get; }

    public HttpResponseData Duplicate()
    {
        return new HttpResponseData(Version, StatusCode, ReasonPhrase, Headers, IsSuccessStatusCode, Content, Bytes);
    }

    public static HttpResponseData FromHttpResponseMessage(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var headers = new List<HttpHeader>();

        foreach ((string key, IEnumerable<string> values) in response.Headers)
        {
            headers.Add(new HttpHeader(key, string.Join(",", values)));
        }

        string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        byte[] bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

        return new HttpResponseData(
            response.Version.ToString(),
            (int)response.StatusCode,
            response.ReasonPhrase,
            headers,
            response.IsSuccessStatusCode,
            content,
            bytes);
    }
}
