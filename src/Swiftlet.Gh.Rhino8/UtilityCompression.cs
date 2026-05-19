using System.IO.Compression;

namespace Swiftlet.Gh.Rhino8;

internal static class UtilityCompression
{
    public static byte[] Compress(byte[] data)
    {
        using var compressed = new MemoryStream();
        using (var source = new MemoryStream(data ?? Array.Empty<byte>()))
        using (var gzip = new GZipStream(compressed, CompressionMode.Compress))
        {
            source.CopyTo(gzip);
        }

        return compressed.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        using var source = new MemoryStream(data ?? Array.Empty<byte>());
        return Decompress(source);
    }

    public static byte[] Decompress(Stream source)
    {
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        gzip.CopyTo(decompressed);
        return decompressed.ToArray();
    }
}
