using System.Text;
using System.Web;
using Swiftlet.Core.Http;

namespace Swiftlet.Runtime;

public static class HttpDeconstructionWorkflow
{
    public sealed record UrlParts(string Base, string Scheme, string Host, string Route, QueryParameter[] Parameters);
    public static UrlParts DeconstructUrl(string url)
    {
        if (!url.StartsWith("http", StringComparison.Ordinal))
            throw new Exception(" A valid URL must include a scheme (http or https)");
        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var parameters = query.AllKeys.Where(key => key is not null)
            .Select(key => new QueryParameter(key!, query[key] ?? string.Empty)).ToArray();
        return new UrlParts(url.Split('?').First(), uri.Scheme, uri.Host, uri.AbsolutePath, parameters);
    }
    public static string DecodeBodyText(byte[] bytes)
    {
        try { return Encoding.UTF8.GetString(bytes); }
        catch { return string.Empty; }
    }
    public static IReadOnlyList<MultipartField>? DeconstructMultipart(IRequestBody body)
    {
        if (body is RequestBodyMultipartForm multipart) return multipart.Fields;
        if (body.ContentType is not null && body.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return MultipartBodyParser.Parse(body.ToByteArray(), body.ContentType);
        return null;
    }
}
