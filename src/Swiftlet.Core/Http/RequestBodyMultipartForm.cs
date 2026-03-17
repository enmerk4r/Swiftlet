using System.Net.Http;

namespace Swiftlet.Core.Http;

public sealed class RequestBodyMultipartForm : IRequestBody
{
    public RequestBodyMultipartForm()
        : this(Array.Empty<MultipartField>())
    {
    }

    public RequestBodyMultipartForm(IEnumerable<MultipartField> fields)
    {
        Fields = fields?.Select(static field => field?.Duplicate() ?? new MultipartField(string.Empty, Array.Empty<byte>()))
            .ToList()
            ?? [];
    }

    public RequestBodyMultipartForm(IEnumerable<KeyValuePair<string, IRequestBody>> fields)
    {
        Fields = fields?.Select(static field => new MultipartField(field.Key, field.Value)).ToList() ?? [];
    }

    public RequestBodyMultipartForm(IEnumerable<IRequestBody> fields)
    {
        Fields = fields?.Select(static field => new MultipartField(string.Empty, field)).ToList() ?? [];
    }

    public RequestBodyMultipartForm(IEnumerable<string> keys, IEnumerable<IRequestBody> fields)
    {
        string[] keyArray = keys?.ToArray() ?? [];
        IRequestBody[] fieldArray = fields?.ToArray() ?? [];

        if (keyArray.Length != fieldArray.Length)
        {
            throw new ArgumentException("The number of keys must match the number of fields.");
        }

        Fields = keyArray
            .Zip(fieldArray, static (key, field) => new MultipartField(key, field))
            .ToList();
    }

    public string ContentType => CompileForm().Headers.ContentType?.ToString() ?? ContentTypes.MultipartForm;

    public object Value => Fields;

    public List<MultipartField> Fields { get; }

    public IRequestBody Duplicate()
    {
        return new RequestBodyMultipartForm(Fields);
    }

    public HttpContent ToHttpContent()
    {
        return CompileForm();
    }

    public MultipartFormDataContent CompileForm()
    {
        var form = new MultipartFormDataContent();

        foreach (MultipartField field in Fields)
        {
            HttpContent content = field.ToHttpContent();

            if (string.IsNullOrEmpty(field.Name))
            {
                form.Add(content);
            }
            else if (!string.IsNullOrEmpty(field.FileName))
            {
                form.Add(content, field.Name, field.FileName);
            }
            else
            {
                form.Add(content, field.Name);
            }
        }

        return form;
    }

    public byte[] ToByteArray()
    {
        using MultipartFormDataContent form = CompileForm();
        using var stream = new MemoryStream();
        form.CopyToAsync(stream).GetAwaiter().GetResult();
        return stream.ToArray();
    }
}
