namespace Swiftlet.Gh.Rhino8;

public sealed class ModernMcpHttpResponse
{
    public ModernMcpHttpResponse(
        int statusCode,
        string? contentType,
        string? body,
        IEnumerable<KeyValuePair<string, string>>? headers = null)
    {
        StatusCode = statusCode;
        ContentType = contentType;
        Body = body;
        Headers = headers?.ToArray() ?? [];
    }

    public int StatusCode { get; }

    public string? ContentType { get; }

    public string? Body { get; }

    public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

    public static ModernMcpHttpResponse Json(string body, IEnumerable<KeyValuePair<string, string>>? headers = null) =>
        new(200, "application/json", body, headers);

    public static ModernMcpHttpResponse Accepted() => new(202, null, null);

    public static ModernMcpHttpResponse PlainText(int statusCode, string body) =>
        new(statusCode, "text/plain; charset=utf-8", body);
}
