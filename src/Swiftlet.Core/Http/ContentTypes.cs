namespace Swiftlet.Core.Http;

public static class ContentTypes
{
    public const string Header = "Content-Type";

    public const string TextPlain = "text/plain";
    public const string JavaScript = "text/javascript";
    public const string ApplicationJson = "application/json";
    public const string TextHtml = "text/html";
    public const string ApplicationXml = "application/xml";
    public const string ApplicationOctetStream = "application/octet-stream";
    public const string MultipartForm = "multipart/form-data";
    public const string FormUrlEncoded = "application/x-www-form-urlencoded";

    public static string ToDisplayName(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            TextPlain => "Text",
            JavaScript => "JavaScript",
            ApplicationJson => "JSON",
            ApplicationXml => "XML",
            TextHtml => "HTML",
            _ => "Custom",
        };
    }
}
