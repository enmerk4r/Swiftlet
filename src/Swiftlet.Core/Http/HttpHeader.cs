namespace Swiftlet.Core.Http;

public sealed class HttpHeader
{
    public HttpHeader(string key, string value)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Key { get; }

    public string Value { get; }
}
