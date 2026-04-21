using System.Text;

namespace Swiftlet.Gh.Rhino8;

internal static class UtilityEncoding
{
    public static Encoding Resolve(string? name)
    {
        string normalized = string.IsNullOrWhiteSpace(name) ? "UTF8" : name.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ASCII" => Encoding.ASCII,
            "UNICODE" => Encoding.Unicode,
            "UTF8" => Encoding.UTF8,
#pragma warning disable SYSLIB0001
            "UTF7" => Encoding.UTF7,
#pragma warning restore SYSLIB0001
            "UTF32" => Encoding.UTF32,
            _ => throw new ArgumentException($"{name} is an unknown encoding"),
        };
    }
}
