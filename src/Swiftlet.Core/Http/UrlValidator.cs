using Swiftlet.Core.Security;

namespace Swiftlet.Core.Http;

public static class UrlValidator
{
    public static UrlValidationResult ValidateHttpUrl(
        string? url,
        IIpBlacklist? ipBlacklist = null,
        Func<string, IPHostEntry>? hostResolver = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlValidationResult.Failure("Invalid URL.");
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return UrlValidationResult.Failure("URL must include a scheme (http:// or https://)");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return UrlValidationResult.Failure("URL is not well formed.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return UrlValidationResult.Failure("Please make sure your URL starts with 'http' or 'https'.");
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            return UrlValidationResult.Failure("Please do not include query parameters in your URL. Use the Params (P) input instead.");
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            return UrlValidationResult.Failure("Please do not include a fragment in your URL.");
        }

        if (ipBlacklist is null)
        {
            return UrlValidationResult.Success();
        }

        return uri.HostNameType switch
        {
            UriHostNameType.Dns => ValidateDnsHost(uri, ipBlacklist, hostResolver),
            UriHostNameType.IPv4 or UriHostNameType.IPv6 => ValidateIpHost(uri, ipBlacklist),
            _ => UrlValidationResult.Failure("The given hostname or IP address is invalid."),
        };
    }

    private static UrlValidationResult ValidateDnsHost(
        Uri uri,
        IIpBlacklist ipBlacklist,
        Func<string, IPHostEntry>? hostResolver)
    {
        try
        {
            IPHostEntry hostEntry = (hostResolver ?? Dns.GetHostEntry)(uri.Host);
            return ipBlacklist.IsHostBlacklisted(hostEntry)
                ? UrlValidationResult.Failure("The given hostname or IP address is blacklisted.")
                : UrlValidationResult.Success();
        }
        catch (SocketException)
        {
            return UrlValidationResult.Failure("Please use a valid hostname or IP address.");
        }
    }

    private static UrlValidationResult ValidateIpHost(Uri uri, IIpBlacklist ipBlacklist)
    {
        if (!IPAddress.TryParse(uri.Host, out IPAddress? address))
        {
            return UrlValidationResult.Failure("Please use a valid hostname or IP address.");
        }

        return ipBlacklist.IsAddressBlacklisted(address)
            ? UrlValidationResult.Failure("The given hostname or IP address is blacklisted.")
            : UrlValidationResult.Success();
    }
}
