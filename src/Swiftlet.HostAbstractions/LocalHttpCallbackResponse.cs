using System.Text;

namespace Swiftlet.HostAbstractions;

public sealed class LocalHttpCallbackResponse
{
    public LocalHttpCallbackResponse(int statusCode, string contentType, string body)
    {
        StatusCode = statusCode;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "text/plain; charset=utf-8" : contentType;
        Body = body ?? string.Empty;
    }

    public int StatusCode { get; }

    public string ContentType { get; }

    public string Body { get; }

    public byte[] GetBodyBytes() => Encoding.UTF8.GetBytes(Body);

    public static LocalHttpCallbackResponse Html(int statusCode, string html) =>
        new(statusCode, "text/html; charset=utf-8", html);
}
