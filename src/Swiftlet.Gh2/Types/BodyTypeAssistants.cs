using System.Text;
using System.Text.Json.Nodes;
using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using GrasshopperIO;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh2.Types;

/// <summary>Byte arrays are passed as plain <c>byte[]</c>; never modify one received from an input.</summary>
public sealed class ByteArrayTypeAssistant : TypeAssistant<byte[]>
{
    public ByteArrayTypeAssistant() : base("Byte Array") { }

    public override byte[] Copy(byte[] instance) => (byte[])instance.Clone();

    public override string DescribePrimary(Pear<byte[]> pear) => Format(pear.Item);

    public override string Format(byte[] instance) => $"BYTE ARRAY [ {instance.Length} ]";

    public override bool Same(byte[] a, byte[] b) => a.AsSpan().SequenceEqual(b);

    public override bool Write(IWriter writer, Name location, byte[] instance)
    {
        writer.Integer8Array(location, instance);
        return true;
    }

    public override bool Read(IReader reader, Name location, out byte[] instance)
    {
        instance = null!;
        if (!reader.HasItem(location))
            return false;

        instance = reader.Integer8Array(location) ?? Array.Empty<byte>();
        return true;
    }
}

public sealed class MultipartFieldTypeAssistant : TypeAssistant<MultipartField>
{
    public MultipartFieldTypeAssistant() : base("Multipart Field") { }

    public override MultipartField Copy(MultipartField instance) => instance.Duplicate();

    public override string DescribePrimary(Pear<MultipartField> pear) => Format(pear.Item);

    public override string Format(MultipartField instance)
    {
        string name = string.IsNullOrWhiteSpace(instance.Name) ? "<unnamed>" : instance.Name;
        return $"MULTIPART FIELD [ {name} | {instance.Bytes?.Length ?? 0} bytes ]";
    }

    public override bool Write(IWriter writer, Name location, MultipartField instance)
    {
        writer.String(location, ToJson(instance).ToJsonString());
        return true;
    }

    public override bool Read(IReader reader, Name location, out MultipartField instance)
    {
        instance = null!;
        if (!reader.HasItem(location) || JsonNode.Parse(reader.String(location)) is not JsonObject json)
            return false;

        instance = FromJson(json);
        return true;
    }

    internal static JsonObject ToJson(MultipartField field)
    {
        return new JsonObject
        {
            ["name"] = field.Name,
            ["fileName"] = field.FileName,
            ["contentType"] = field.ContentType,
            ["isText"] = field.IsText,
            ["text"] = field.Text,
            ["encoding"] = field.Encoding?.WebName,
            ["bytes"] = Convert.ToBase64String(field.Bytes ?? Array.Empty<byte>()),
        };
    }

    internal static MultipartField FromJson(JsonObject json)
    {
        string name = (string?)json["name"] ?? string.Empty;
        string? contentType = (string?)json["contentType"];
        if ((bool?)json["isText"] == true)
        {
            string? encodingName = (string?)json["encoding"];
            Encoding encoding = encodingName is null ? Encoding.UTF8 : Encoding.GetEncoding(encodingName);
            return new MultipartField(name, (string?)json["text"] ?? string.Empty, contentType, encoding);
        }

        byte[] bytes = Convert.FromBase64String((string?)json["bytes"] ?? string.Empty);
        return new MultipartField(name, bytes, (string?)json["fileName"], contentType);
    }
}

public sealed class RequestBodyTypeAssistant : TypeAssistant<IRequestBody>
{
    public RequestBodyTypeAssistant() : base("Request Body") { }

    public override IRequestBody Copy(IRequestBody instance) => instance.Duplicate();

    public override string DescribePrimary(Pear<IRequestBody> pear) => Format(pear.Item);

    public override string Format(IRequestBody instance) => $"REQUEST BODY [ {instance.ContentType} ]";

    public override bool Write(IWriter writer, Name location, IRequestBody instance)
    {
        JsonObject json;
        switch (instance)
        {
            case RequestBodyText text:
                json = new JsonObject { ["kind"] = "text", ["contentType"] = text.ContentType, ["text"] = text.Text };
                break;
            case RequestBodyFormUrlEncoded form:
                var pairs = new JsonArray();
                foreach (KeyValuePair<string, string> pair in form.FormData)
                    pairs.Add(new JsonArray(pair.Key, pair.Value));
                json = new JsonObject { ["kind"] = "form", ["pairs"] = pairs };
                break;
            case RequestBodyMultipartForm multipart:
                var fields = new JsonArray();
                foreach (MultipartField field in multipart.Fields)
                    fields.Add(MultipartFieldTypeAssistant.ToJson(field));
                json = new JsonObject { ["kind"] = "multipart", ["fields"] = fields };
                break;
            default:
                // RequestBodyBytes and any future body type: keep the content type and exact bytes.
                json = new JsonObject
                {
                    ["kind"] = "bytes",
                    ["contentType"] = instance.ContentType,
                    ["bytes"] = Convert.ToBase64String(instance.ToByteArray()),
                };
                break;
        }

        writer.String(location, json.ToJsonString());
        return true;
    }

    public override bool Read(IReader reader, Name location, out IRequestBody instance)
    {
        instance = null!;
        if (!reader.HasItem(location) || JsonNode.Parse(reader.String(location)) is not JsonObject json)
            return false;

        switch ((string?)json["kind"])
        {
            case "text":
                instance = new RequestBodyText((string?)json["contentType"] ?? string.Empty, (string?)json["text"] ?? string.Empty);
                return true;
            case "form":
                var pairs = new List<KeyValuePair<string, string>>();
                foreach (JsonNode? pair in json["pairs"] as JsonArray ?? new JsonArray())
                {
                    if (pair is JsonArray { Count: 2 } entry)
                        pairs.Add(new KeyValuePair<string, string>((string?)entry[0] ?? string.Empty, (string?)entry[1] ?? string.Empty));
                }

                instance = new RequestBodyFormUrlEncoded(pairs);
                return true;
            case "multipart":
                var fields = new List<MultipartField>();
                foreach (JsonNode? field in json["fields"] as JsonArray ?? new JsonArray())
                {
                    if (field is JsonObject fieldJson)
                        fields.Add(MultipartFieldTypeAssistant.FromJson(fieldJson));
                }

                instance = new RequestBodyMultipartForm(fields);
                return true;
            case "bytes":
                instance = new RequestBodyBytes(
                    (string?)json["contentType"] ?? string.Empty,
                    Convert.FromBase64String((string?)json["bytes"] ?? string.Empty));
                return true;
            default:
                return false;
        }
    }
}
