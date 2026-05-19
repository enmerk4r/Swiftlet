namespace Swiftlet.Core.Http;

public static class UrlBuilder
{
    public static string AddQueryParameters(string url, IEnumerable<QueryParameter>? parameters)
    {
        Guard.ThrowIfNullOrWhiteSpace(url, nameof(url));

        if (parameters is null)
        {
            return url;
        }

        string[] queryParts = parameters
            .Select(parameter => parameter?.ToQueryString())
            .Where(query => !string.IsNullOrWhiteSpace(query))
            .Cast<string>()
            .ToArray();

        if (queryParts.Length == 0)
        {
            return url;
        }

        char separator = url.Contains('?') ? '&' : '?';
        return string.Concat(url, separator, string.Join("&", queryParts));
    }
}
