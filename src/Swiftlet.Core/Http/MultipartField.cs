using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Swiftlet.Core.Http;

public sealed class MultipartField
{
    public MultipartField(string name, byte[] bytes, string? fileName = null, string? contentType = null)
    {
        Name = name ?? string.Empty;
        FileName = fileName;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? ContentTypes.ApplicationOctetStream : contentType;
        Bytes = bytes?.ToArray() ?? [];
        IsText = false;
        Encoding = null;
        Text = null;
    }

    public MultipartField(string name, string text, string? contentType = null, Encoding? encoding = null)
    {
        Name = name ?? string.Empty;
        FileName = null;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? ContentTypes.TextPlain : contentType;
        Encoding = encoding ?? Encoding.UTF8;
        Text = text ?? string.Empty;
        Bytes = Encoding.GetBytes(Text);
        IsText = true;
    }

    public MultipartField(string name, IRequestBody body)
    {
        ArgumentNullException.ThrowIfNull(body);

        Name = name ?? string.Empty;
        FileName = null;
        ContentType = string.IsNullOrWhiteSpace(body.ContentType) ? ContentTypes.ApplicationOctetStream : body.ContentType;
        Bytes = body.ToByteArray();
        IsText = false;
        Encoding = null;
        Text = null;
    }

    public string Name { get; }

    public string? FileName { get; }

    public string ContentType { get; }

    public byte[] Bytes { get; }

    public bool IsText { get; }

    public Encoding? Encoding { get; }

    public string? Text { get; }

    public MultipartField Duplicate()
    {
        return IsText
            ? new MultipartField(Name, Text ?? string.Empty, ContentType, Encoding)
            : new MultipartField(Name, Bytes, FileName, ContentType);
    }

    public HttpContent ToHttpContent()
    {
        if (IsText)
        {
            var content = new StringContent(Text ?? string.Empty, Encoding ?? Encoding.UTF8, ContentType);
            content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
            return content;
        }

        var byteContent = new ByteArrayContent(Bytes);
        if (!string.IsNullOrWhiteSpace(ContentType))
        {
            byteContent.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
        }

        return byteContent;
    }
}
