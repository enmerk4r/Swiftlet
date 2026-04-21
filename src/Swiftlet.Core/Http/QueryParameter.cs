namespace Swiftlet.Core.Http;

public sealed class QueryParameter
{
    public QueryParameter(string key, string value)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Key { get; }

    public string Value { get; }

    public string ToQueryString()
    {
        return $"{Uri.EscapeDataString(Key)}={Uri.EscapeDataString(Value)}";
    }
}
