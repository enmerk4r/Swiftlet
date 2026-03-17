using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8;

public sealed class ModernServerHttpResponse
{
    public ModernServerHttpResponse(
        int statusCode,
        string? contentType,
        byte[]? body,
        IEnumerable<HttpHeader>? headers = null)
    {
        StatusCode = statusCode;
        ContentType = contentType;
        Body = body?.ToArray() ?? [];
        Headers = headers?.Select(static header => new HttpHeader(header.Key, header.Value)).ToArray() ?? [];
    }

    public int StatusCode { get; }

    public string? ContentType { get; }

    public byte[] Body { get; }

    public IReadOnlyList<HttpHeader> Headers { get; }

    public static ModernServerHttpResponse Empty(int statusCode = 200, IEnumerable<HttpHeader>? headers = null) =>
        new(statusCode, null, null, headers);

    public static ModernServerHttpResponse FromBody(
        int statusCode,
        IRequestBody? body,
        IEnumerable<HttpHeader>? headers = null)
    {
        return new(
            statusCode,
            body?.ContentType,
            body?.ToByteArray(),
            headers);
    }

    public static ModernServerHttpResponse PlainText(int statusCode, string body, IEnumerable<HttpHeader>? headers = null) =>
        new(statusCode, ContentTypes.TextPlain, body is null ? [] : System.Text.Encoding.UTF8.GetBytes(body), headers);
}
