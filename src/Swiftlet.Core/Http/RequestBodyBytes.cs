using System.Net.Http;
using System.Net.Http.Headers;

namespace Swiftlet.Core.Http;

public sealed class RequestBodyBytes : IRequestBody
{
    public RequestBodyBytes(string contentType, byte[] content)
    {
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        Content = content?.ToArray() ?? [];
    }

    public string ContentType { get; }

    public object Value => Content;

    public byte[] Content { get; }

    public IRequestBody Duplicate()
    {
        return new RequestBodyBytes(ContentType, Content);
    }

    public HttpContent ToHttpContent()
    {
        var content = new ByteArrayContent(Content);
        content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
        return content;
    }

    public byte[] ToByteArray()
    {
        return Content.ToArray();
    }
}
