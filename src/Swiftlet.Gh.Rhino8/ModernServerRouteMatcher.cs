namespace Swiftlet.Gh.Rhino8;

internal static class ModernServerRouteMatcher
{
    public static string NormalizeRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "/";
        }

        string normalized = route.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized.ToLowerInvariant();
    }

    public static IReadOnlyList<string> NormalizeRoutes(IEnumerable<string>? routes)
    {
        return routes?
            .Select(NormalizeRoute)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(static route => route.Length)
            .ToArray()
            ?? ["/"];
    }

    public static string? FindBestMatch(string requestPath, IEnumerable<string>? routes)
    {
        string normalizedPath = NormalizeRoute(requestPath);
        string? bestMatch = null;
        int bestLength = -1;

        foreach (string route in NormalizeRoutes(routes))
        {
            if (string.Equals(normalizedPath, route, StringComparison.Ordinal))
            {
                return route;
            }

            if (normalizedPath.StartsWith(route, StringComparison.Ordinal) && route.Length > bestLength)
            {
                bestMatch = route;
                bestLength = route.Length;
            }
        }

        return bestMatch;
    }
}
