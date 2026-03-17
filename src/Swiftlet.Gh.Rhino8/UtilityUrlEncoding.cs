using System.Text;

namespace Swiftlet.Gh.Rhino8;

internal static class UtilityUrlEncoding
{
    public static string Encode(string? text, Encoding encoding)
    {
        byte[] bytes = encoding.GetBytes(text ?? string.Empty);
        var builder = new StringBuilder(bytes.Length * 3);

        foreach (byte value in bytes)
        {
            if (IsUnreserved(value))
            {
                builder.Append((char)value);
            }
            else if (value == 0x20)
            {
                builder.Append('+');
            }
            else
            {
                builder.Append('%');
                builder.Append(value.ToString("X2"));
            }
        }

        return builder.ToString();
    }

    private static bool IsUnreserved(byte value)
    {
        return (value >= 'A' && value <= 'Z') ||
               (value >= 'a' && value <= 'z') ||
               (value >= '0' && value <= '9') ||
               value is (byte)'-' or (byte)'_' or (byte)'.' or (byte)'*';
    }
}
