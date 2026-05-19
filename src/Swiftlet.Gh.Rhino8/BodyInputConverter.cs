using System.Text;
using Grasshopper.Kernel.Types;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8;

internal static class BodyInputConverter
{
    public static string ToLegacyText(object? input)
    {
        object? value = Unwrap(input);
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            GH_String grasshopperString => grasshopperString.ToString(),
            string text => text,
            JsonArrayGoo arrayGoo => arrayGoo.Value?.ToJsonString() ?? string.Empty,
            JsonObjectGoo objectGoo => objectGoo.Value?.ToJsonString() ?? string.Empty,
            JsonNodeGoo nodeGoo => nodeGoo.Value?.ToJsonString() ?? "null",
            JsonValueGoo valueGoo => valueGoo.Value?.ToJsonString() ?? "null",
            XmlNodeGoo xmlGoo when xmlGoo.Value is not null => xmlGoo.Value.OuterXml,
            HtmlNodeGoo htmlGoo when htmlGoo.Value is not null => htmlGoo.Value.OuterHtml,
            _ => throw new Exception("Content must be a string, JObject, JArray, XML Node, or HTML Node"),
        };
    }

    public static byte[] ToLegacyBytes(object? input)
    {
        object? value = Unwrap(input);
        if (value is null)
        {
            return Array.Empty<byte>();
        }

        return value switch
        {
            ByteArrayGoo byteArrayGoo => byteArrayGoo.Value ?? Array.Empty<byte>(),
            byte[] bytes => bytes.ToArray(),
            GH_String grasshopperString => Encoding.UTF8.GetBytes(grasshopperString.Value ?? string.Empty),
            string text => Encoding.UTF8.GetBytes(text),
            JsonArrayGoo arrayGoo => Encoding.UTF8.GetBytes(arrayGoo.Value?.ToJsonString() ?? string.Empty),
            JsonObjectGoo objectGoo => Encoding.UTF8.GetBytes(objectGoo.Value?.ToJsonString() ?? string.Empty),
            JsonNodeGoo nodeGoo => Encoding.UTF8.GetBytes(nodeGoo.Value?.ToJsonString() ?? "null"),
            JsonValueGoo valueGoo => Encoding.UTF8.GetBytes(valueGoo.Value?.ToJsonString() ?? "null"),
            XmlNodeGoo xmlGoo when xmlGoo.Value is not null => Encoding.UTF8.GetBytes(xmlGoo.Value.OuterXml),
            HtmlNodeGoo htmlGoo when htmlGoo.Value is not null => Encoding.UTF8.GetBytes(htmlGoo.Value.OuterHtml),
            _ => throw new Exception("Content must be a byte array, string, JObject, JArray, XML Node, or HTML Node"),
        };
    }

    public static string ToText(object? input)
    {
        object? value = Unwrap(input);
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            string text => text,
            GH_String grasshopperString => grasshopperString.Value ?? string.Empty,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ByteArrayGoo byteArrayGoo => Encoding.UTF8.GetString(byteArrayGoo.Value ?? Array.Empty<byte>()),
            RequestBodyGoo requestBodyGoo when requestBodyGoo.Value is not null => Encoding.UTF8.GetString(requestBodyGoo.Value.ToByteArray()),
            IRequestBody requestBody => Encoding.UTF8.GetString(requestBody.ToByteArray()),
            _ => value.ToString() ?? string.Empty,
        };
    }

    public static byte[] ToBytes(object? input)
    {
        object? value = Unwrap(input);
        if (value is null)
        {
            return Array.Empty<byte>();
        }

        return value switch
        {
            byte[] bytes => bytes.ToArray(),
            ByteArrayGoo byteArrayGoo => byteArrayGoo.Value?.ToArray() ?? Array.Empty<byte>(),
            string text => Encoding.UTF8.GetBytes(text),
            GH_String grasshopperString => Encoding.UTF8.GetBytes(grasshopperString.Value ?? string.Empty),
            RequestBodyGoo requestBodyGoo when requestBodyGoo.Value is not null => requestBodyGoo.Value.ToByteArray(),
            IRequestBody requestBody => requestBody.ToByteArray(),
            _ => Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty),
        };
    }

    private static object? Unwrap(object? input)
    {
        return input switch
        {
            GH_ObjectWrapper wrapper => wrapper.Value,
            _ => input,
        };
    }
}
