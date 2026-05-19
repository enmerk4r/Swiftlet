using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Swiftlet.Core.Http;

public sealed class RequestBodyText : IRequestBody
{
    public RequestBodyText()
        : this(ContentTypes.TextPlain, string.Empty)
    {
    }

    public RequestBodyText(string contentType, string text)
    {
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        Text = text ?? string.Empty;
    }

    public string ContentType { get; }

    public object Value => Text;

    public string Text { get; }

    public IRequestBody Duplicate()
    {
        return new RequestBodyText(ContentType, Text);
    }

    public HttpContent ToHttpContent()
    {
        var content = new StringContent(Text, Encoding.UTF8, ContentType);
        content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
        return content;
    }

    public byte[] ToByteArray()
    {
        return Encoding.UTF8.GetBytes(Text);
    }
}
