using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using Swiftlet.Core.Http;
using Swiftlet.Core.Json;

namespace Swiftlet.Runtime;

/// <summary>
/// Host-neutral conversion of request-body content (the plain values a GH2 component receives) to text
/// or bytes. Mirrors the GH1 shell's BodyInputConverter.ToLegacyText/ToLegacyBytes, which do the same
/// for Grasshopper 1 goo.
/// </summary>
internal static class RequestBodyContent
{
    private const string TextError = "Content must be a string, JObject, JArray, XML Node, or HTML Node";
    private const string BytesError = "Content must be a byte array, string, JObject, JArray, XML Node, or HTML Node";

    /// <summary>Text of a body input; null input is empty text.</summary>
    public static string ToText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            _ => TryText(value, out string text) ? text : throw new InvalidOperationException(TextError),
        };
    }

    /// <summary>Bytes of a body input: byte arrays are copied, everything else is the UTF-8 of its text.</summary>
    public static byte[] ToBytes(object? value)
    {
        return value switch
        {
            null => Array.Empty<byte>(),
            byte[] bytes => bytes.ToArray(),
            _ => TryText(value, out string text) ? Encoding.UTF8.GetBytes(text) : throw new InvalidOperationException(BytesError),
        };
    }

    /// <summary>Content type implied by structured content (JSON, XML, HTML); null for plain text.</summary>
    public static string? DetectContentType(object? value)
    {
        return value switch
        {
            JsonData or JsonNode => ContentTypes.ApplicationJson,
            XmlNode => ContentTypes.ApplicationXml,
            HtmlAgilityPack.HtmlNode => ContentTypes.TextHtml,
            _ => null,
        };
    }

    private static bool TryText(object value, out string text)
    {
        switch (value)
        {
            case string s:
                text = s;
                return true;
            case JsonData json:
                text = json.Json; // compact JSON; JSON null is "null"
                return true;
            case JsonNode node:
                text = node.ToJsonString();
                return true;
            case XmlNode xml:
                lock (xml.OwnerDocument ?? (object)xml)
                    text = xml.OuterXml;
                return true;
            case HtmlAgilityPack.HtmlNode html:
                lock (html.OwnerDocument ?? (object)html)
                    text = html.OuterHtml;
                return true;
            default:
                text = string.Empty;
                return false;
        }
    }
}
