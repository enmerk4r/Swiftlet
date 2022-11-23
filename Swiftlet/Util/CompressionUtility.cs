using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Util
{
    public static class CompressionUtility
    {
        public static byte[] Compress(byte[] data)
        {
            using (var compressed = new MemoryStream())
            {
                using (var source = new MemoryStream(data))
                {
                    using (var gzip = new GZipStream(compressed, CompressionMode.Compress))
                    {
                        source.CopyTo(gzip);
                    }
                }
                return compressed.ToArray();
            }
        }

        public static byte[] CompressString(string s, Encoding encoding)
        {
            return Compress(encoding.GetBytes(s));
        }

        public static string CompressStringToBase64(string s, Encoding encoding)
        {
            return Convert.ToBase64String(CompressString(s, encoding));
        }

        public static byte[] Decompress(Stream source)
        {
            using (var gzip = new GZipStream(source, CompressionMode.Decompress))
            {
                using (var decompressed = new MemoryStream())
                {
                    gzip.CopyTo(decompressed);
                    return decompressed.ToArray();
                }
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return Decompress(ms);
            }
        }

        public static string DecompressString(Stream source, Encoding encoding)
        {
            return encoding.GetString(Decompress(source));
        }
    }
}
