using System.Text;
using Swiftlet.Core.Http;

namespace Swiftlet.Runtime;

/// <summary>Authentication header construction shared by both Grasshopper shells.</summary>
public static class AuthHeaderWorkflow
{
    // Preserve the GH1 ASCII encoding contract, including replacement of non-ASCII characters.
    public static HttpHeader Basic(string userName, string password) =>
        new("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"))}");
    public static HttpHeader Bearer(string token) => new("Authorization", $"Bearer {token}");
    public static HttpHeader ApiHeader(string key, string value) => new(key, value);
    public static QueryParameter ApiQuery(string key, string value) => new(key, value);
}

