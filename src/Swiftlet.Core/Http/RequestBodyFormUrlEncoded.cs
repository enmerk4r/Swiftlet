using System.Net.Http;
using System.Text;

namespace Swiftlet.Core.Http;

public sealed class RequestBodyFormUrlEncoded : IRequestBody
{
    private readonly IReadOnlyList<KeyValuePair<string, string>> _formData;

    public RequestBodyFormUrlEncoded()
        : this([])
    {
    }

    public RequestBodyFormUrlEncoded(IEnumerable<KeyValuePair<string, string>> formData)
    {
        _formData = formData?.ToList() ?? [];
    }

    public RequestBodyFormUrlEncoded(IEnumerable<string> keys, IEnumerable<string> values)
    {
        string[] keyArray = keys?.ToArray() ?? [];
        string[] valueArray = values?.ToArray() ?? [];

        if (keyArray.Length != valueArray.Length)
        {
            throw new ArgumentException("Keys and values must have the same count.");
        }

        _formData = keyArray
            .Zip(valueArray, static (key, value) => new KeyValuePair<string, string>(key, value))
            .ToArray();
    }

    public string ContentType => ContentTypes.FormUrlEncoded;

    public object Value => _formData;

    public IReadOnlyList<KeyValuePair<string, string>> FormData => _formData;

    public IRequestBody Duplicate()
    {
        return new RequestBodyFormUrlEncoded(_formData);
    }

    public HttpContent ToHttpContent()
    {
        return new FormUrlEncodedContent(_formData);
    }

    public byte[] ToByteArray()
    {
        return Encoding.UTF8.GetBytes(ToString());
    }

    public override string ToString()
    {
        return string.Join(
            "&",
            _formData.Select(static pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }
}
