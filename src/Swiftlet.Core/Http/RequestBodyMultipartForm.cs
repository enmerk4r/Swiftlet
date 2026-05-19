using System.Net.Http;

namespace Swiftlet.Core.Http;

public sealed class RequestBodyMultipartForm : IRequestBody
{
    private readonly string _boundary;

    public RequestBodyMultipartForm()
        : this(Array.Empty<MultipartField>(), CreateBoundary())
    {
    }

    public RequestBodyMultipartForm(IEnumerable<MultipartField> fields)
        : this(fields, CreateBoundary())
    {
    }

    private RequestBodyMultipartForm(IEnumerable<MultipartField> fields, string boundary)
    {
        _boundary = boundary;
        Fields = fields?.Select(static field => field?.Duplicate() ?? new MultipartField(string.Empty, Array.Empty<byte>()))
            .ToList()
            ?? [];
    }

    public RequestBodyMultipartForm(IEnumerable<KeyValuePair<string, IRequestBody>> fields)
        : this(
            fields?.Select(static field => new MultipartField(field.Key, field.Value)).ToList() ?? [],
            CreateBoundary())
    {
    }

    public RequestBodyMultipartForm(IEnumerable<IRequestBody> fields)
        : this(
            fields?.Select(static field => new MultipartField(string.Empty, field)).ToList() ?? [],
            CreateBoundary())
    {
    }

    public RequestBodyMultipartForm(IEnumerable<string> keys, IEnumerable<IRequestBody> fields)
        : this(ZipFields(keys, fields), CreateBoundary())
    {
    }

    private static List<MultipartField> ZipFields(IEnumerable<string> keys, IEnumerable<IRequestBody> fields)
    {
        string[] keyArray = keys?.ToArray() ?? [];
        IRequestBody[] fieldArray = fields?.ToArray() ?? [];

        if (keyArray.Length != fieldArray.Length)
        {
            throw new ArgumentException("The number of keys must match the number of fields.");
        }

        return keyArray
            .Zip(fieldArray, static (key, field) => new MultipartField(key, field))
            .ToList();
    }

    public string ContentType => CompileForm().Headers.ContentType?.ToString() ?? ContentTypes.MultipartForm;

    public object Value => Fields;

    public List<MultipartField> Fields { get; }

    public IRequestBody Duplicate()
    {
        return new RequestBodyMultipartForm(Fields, _boundary);
    }

    public HttpContent ToHttpContent()
    {
        return CompileForm();
    }

    public MultipartFormDataContent CompileForm()
    {
        var form = new MultipartFormDataContent(_boundary);

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

    private static string CreateBoundary()
    {
        return Guid.NewGuid().ToString("N");
    }

    public byte[] ToByteArray()
    {
        using MultipartFormDataContent form = CompileForm();
        using var stream = new MemoryStream();
        form.CopyToAsync(stream).GetAwaiter().GetResult();
        return stream.ToArray();
    }
}
