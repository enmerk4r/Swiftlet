using System.Net.Http.Headers;
using System.Text;

namespace Swiftlet.Core.Http;

public static class MultipartBodyParser
{
    public static List<MultipartField> Parse(byte[] bytes, string contentType)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        string boundary = ExtractBoundary(contentType)
            ?? throw new FormatException("Could not extract boundary from content type.");

        var fields = new List<MultipartField>();
        byte[] delimiter = Encoding.ASCII.GetBytes("--" + boundary);
        int boundaryIndex = FindBoundary(bytes, delimiter, 0);

        if (boundaryIndex < 0)
        {
            return fields;
        }

        while (boundaryIndex >= 0 && boundaryIndex < bytes.Length)
        {
            int cursor = boundaryIndex + delimiter.Length;
            if (HasFinalBoundarySuffix(bytes, cursor))
            {
                break;
            }

            cursor = SkipLineEnding(bytes, cursor);
            if (cursor < 0)
            {
                throw new FormatException("Malformed multipart boundary delimiter.");
            }

            int separatorLength;
            int headerEnd = FindHeaderSeparator(bytes, cursor, out separatorLength);
            if (headerEnd < 0)
            {
                throw new FormatException("Malformed multipart headers.");
            }

            string headers = Encoding.Latin1.GetString(bytes, cursor, headerEnd - cursor);
            int contentStart = headerEnd + separatorLength;
            int nextBoundary = FindBoundary(bytes, delimiter, contentStart);
            if (nextBoundary < 0)
            {
                throw new FormatException("Multipart closing boundary not found.");
            }

            int contentEnd = TrimBoundaryLineEnding(bytes, nextBoundary);
            if (contentEnd < contentStart)
            {
                contentEnd = contentStart;
            }

            byte[] partBytes = bytes.AsSpan(contentStart, contentEnd - contentStart).ToArray();
            fields.Add(ParsePart(headers, partBytes));
            boundaryIndex = nextBoundary;
        }

        return fields;
    }

    private static MultipartField ParsePart(string headers, byte[] bodyBytes)
    {
        string name = string.Empty;
        string? fileName = null;
        string? partContentType = null;
        Encoding? encoding = null;

        foreach (string headerLine in headers.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = headerLine.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string headerName = headerLine[..separatorIndex].Trim();
            string headerValue = headerLine[(separatorIndex + 1)..].Trim();

            if (headerName.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase))
            {
                ParseDisposition(headerValue, ref name, ref fileName);
            }
            else if (headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                partContentType = headerValue;
                encoding = TryGetEncoding(headerValue);
            }
        }

        bool isText = string.IsNullOrEmpty(fileName) && IsTextContentType(partContentType);
        if (isText)
        {
            Encoding textEncoding = encoding ?? Encoding.UTF8;
            return new MultipartField(name, textEncoding.GetString(bodyBytes), partContentType, textEncoding);
        }

        return new MultipartField(name, bodyBytes, fileName, partContentType);
    }

    private static void ParseDisposition(string headerValue, ref string name, ref string? fileName)
    {
        if (ContentDispositionHeaderValue.TryParse(headerValue, out ContentDispositionHeaderValue? disposition))
        {
            if (!string.IsNullOrEmpty(disposition.Name))
            {
                name = TrimQuotes(disposition.Name);
            }

            string? parsedFileName = disposition.FileNameStar;
            if (string.IsNullOrEmpty(parsedFileName))
            {
                parsedFileName = disposition.FileName;
            }

            if (!string.IsNullOrEmpty(parsedFileName))
            {
                fileName = TrimQuotes(parsedFileName);
            }
        }
    }

    private static string? ExtractBoundary(string contentType)
    {
        if (MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? mediaTypeHeader))
        {
            NameValueHeaderValue? boundary = mediaTypeHeader.Parameters
                .FirstOrDefault(static parameter => parameter.Name.Equals("boundary", StringComparison.OrdinalIgnoreCase));
            return boundary?.Value?.Trim().Trim('"');
        }

        return null;
    }

    private static Encoding? TryGetEncoding(string contentType)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? mediaTypeHeader)
            || string.IsNullOrWhiteSpace(mediaTypeHeader.CharSet))
        {
            return null;
        }

        try
        {
            return Encoding.GetEncoding(mediaTypeHeader.CharSet);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool IsTextContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        string mediaType = contentType;
        if (MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? mediaTypeHeader)
            && !string.IsNullOrWhiteSpace(mediaTypeHeader.MediaType))
        {
            mediaType = mediaTypeHeader.MediaType;
        }

        if (mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return mediaType.Equals(ContentTypes.ApplicationJson, StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals(ContentTypes.ApplicationXml, StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals(ContentTypes.JavaScript, StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals(ContentTypes.FormUrlEncoded, StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase);
    }

    private static int FindBoundary(byte[] bytes, byte[] delimiter, int startIndex)
    {
        for (int i = startIndex; i <= bytes.Length - delimiter.Length; i++)
        {
            if (i > 0 && bytes[i - 1] != '\n')
            {
                continue;
            }

            if (Matches(bytes, i, delimiter))
            {
                int suffixIndex = i + delimiter.Length;
                if (suffixIndex < bytes.Length
                    && bytes[suffixIndex] != '\r'
                    && bytes[suffixIndex] != '\n'
                    && !(bytes[suffixIndex] == '-'
                        && suffixIndex + 1 < bytes.Length
                        && bytes[suffixIndex + 1] == '-'))
                {
                    continue;
                }

                return i;
            }
        }

        return -1;
    }

    private static int FindHeaderSeparator(byte[] bytes, int startIndex, out int separatorLength)
    {
        for (int i = startIndex; i < bytes.Length - 1; i++)
        {
            if (i + 3 < bytes.Length
                && bytes[i] == '\r'
                && bytes[i + 1] == '\n'
                && bytes[i + 2] == '\r'
                && bytes[i + 3] == '\n')
            {
                separatorLength = 4;
                return i;
            }

            if (bytes[i] == '\n' && bytes[i + 1] == '\n')
            {
                separatorLength = 2;
                return i;
            }
        }

        separatorLength = 0;
        return -1;
    }

    private static int SkipLineEnding(byte[] bytes, int startIndex)
    {
        if (startIndex + 1 < bytes.Length && bytes[startIndex] == '\r' && bytes[startIndex + 1] == '\n')
        {
            return startIndex + 2;
        }

        if (startIndex < bytes.Length && bytes[startIndex] == '\n')
        {
            return startIndex + 1;
        }

        return -1;
    }

    private static int TrimBoundaryLineEnding(byte[] bytes, int boundaryIndex)
    {
        if (boundaryIndex >= 2 && bytes[boundaryIndex - 2] == '\r' && bytes[boundaryIndex - 1] == '\n')
        {
            return boundaryIndex - 2;
        }

        if (boundaryIndex >= 1 && bytes[boundaryIndex - 1] == '\n')
        {
            return boundaryIndex - 1;
        }

        return boundaryIndex;
    }

    private static bool HasFinalBoundarySuffix(byte[] bytes, int startIndex)
    {
        return startIndex + 1 < bytes.Length && bytes[startIndex] == '-' && bytes[startIndex + 1] == '-';
    }

    private static bool Matches(byte[] bytes, int startIndex, byte[] expected)
    {
        if (startIndex + expected.Length > bytes.Length)
        {
            return false;
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (bytes[startIndex + i] != expected[i])
            {
                return false;
            }
        }

        return true;
    }

    private static string TrimQuotes(string value)
    {
        return value.Trim().Trim('"');
    }
}
