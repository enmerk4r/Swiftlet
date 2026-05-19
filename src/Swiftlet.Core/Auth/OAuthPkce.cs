using System.Security.Cryptography;
using System.Text;

namespace Swiftlet.Core.Auth;

public static class OAuthPkce
{
    public static string GenerateCodeVerifier(int byteCount = 32)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Base64UrlEncode(bytes);
    }

    public static string GenerateCodeChallenge(string codeVerifier)
    {
        Guard.ThrowIfNullOrWhiteSpace(codeVerifier, nameof(codeVerifier));

        byte[] bytes = Encoding.ASCII.GetBytes(codeVerifier);
        byte[] hash = SHA256.HashData(bytes);
        return Base64UrlEncode(hash);
    }

    public static string GenerateState(int byteCount = 16)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Base64UrlEncode(bytes);
    }

    public static string Base64UrlEncode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
