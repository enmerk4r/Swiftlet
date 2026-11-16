using System.Text.Json;
using System.Text.Json.Nodes;

namespace Swiftlet.Core.Json;

/// <summary>
/// Immutable JSON payloads for hosts that share values between threads (Grasshopper 2).
/// <see cref="JsonNode"/> is mutable, single-parent and lazily materialised, so it is not safe to hand
/// the same instance to several consumers. These types keep the compact JSON text and give every
/// caller its own freshly parsed node.
/// </summary>
public abstract class JsonData : IEquatable<JsonData>
{
    private protected JsonData(string json)
    {
        Json = json;
    }

    /// <summary>Compact JSON text. JSON null is the text <c>null</c>.</summary>
    public string Json { get; }

    /// <summary>True when this is the JSON literal null.</summary>
    public bool IsNull => Json == "null";

    /// <summary>Returns a new, unshared node. JSON null gives a null reference.</summary>
    public JsonNode? ToNode() => JsonNode.Parse(Json);

    public bool Equals(JsonData? other) => other is not null && other.GetType() == GetType() && other.Json == Json;

    public override bool Equals(object? obj) => Equals(obj as JsonData);

    public override int GetHashCode() => Json.GetHashCode(StringComparison.Ordinal);

    /// <summary>Same text Grasshopper 1 shows for the equivalent value.</summary>
    public override string ToString() => Describe(Json);

    internal static string Serialize(JsonNode? node) => node is null ? "null" : node.ToJsonString();

    internal static JsonValueKind KindOf(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind;
    }

    private static string Describe(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        switch (root.ValueKind)
        {
            case JsonValueKind.Null:
                return "JSON Value [null]";
            case JsonValueKind.Object:
                int keys = 0;
                foreach (JsonProperty _ in root.EnumerateObject())
                {
                    keys++;
                }

                return $"JSON Object [{keys} keys]";
            case JsonValueKind.Array:
                return $"JSON Array [{root.GetArrayLength()} items]";
            default:
                return $"JSON Value [{(json.Length > 20 ? json[..20] + "..." : json)}]";
        }
    }
}

/// <summary>Any JSON: object, array, scalar or null.</summary>
public sealed class JsonToken : JsonData
{
    public static readonly JsonToken Null = new("null");

    private JsonToken(string json)
        : base(json)
    {
    }

    public JsonToken(JsonNode? node)
        : base(Serialize(node))
    {
    }

    public JsonValueKind Kind => KindOf(Json);

    /// <summary>Parses JSON text; throws <see cref="JsonException"/> when it is not valid JSON.</summary>
    public static JsonToken Parse(string json) => new(JsonNode.Parse(json));

    public static JsonToken From(JsonData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return data as JsonToken ?? new JsonToken(data.Json);
    }
}

/// <summary>A JSON object.</summary>
public sealed class JsonObjectData : JsonData
{
    public static readonly JsonObjectData Empty = new("{}");

    private JsonObjectData(string json)
        : base(json)
    {
    }

    public JsonObjectData(JsonObject? value)
        : base(value is null ? "{}" : value.ToJsonString())
    {
    }

    /// <summary>Returns a new, unshared object.</summary>
    public JsonObject ToObject() => (JsonObject)JsonNode.Parse(Json)!;

    public static bool TryFrom(JsonData data, out JsonObjectData? result)
    {
        ArgumentNullException.ThrowIfNull(data);
        result = data as JsonObjectData ?? (KindOf(data.Json) == JsonValueKind.Object ? new JsonObjectData(data.Json) : null);
        return result is not null;
    }
}

/// <summary>A JSON array.</summary>
public sealed class JsonArrayData : JsonData
{
    public static readonly JsonArrayData Empty = new("[]");

    private JsonArrayData(string json)
        : base(json)
    {
    }

    public JsonArrayData(JsonArray? value)
        : base(value is null ? "[]" : value.ToJsonString())
    {
    }

    /// <summary>Returns a new, unshared array.</summary>
    public JsonArray ToArray() => (JsonArray)JsonNode.Parse(Json)!;

    public static bool TryFrom(JsonData data, out JsonArrayData? result)
    {
        ArgumentNullException.ThrowIfNull(data);
        result = data as JsonArrayData ?? (KindOf(data.Json) == JsonValueKind.Array ? new JsonArrayData(data.Json) : null);
        return result is not null;
    }
}

/// <summary>A JSON scalar: string, number, boolean or null.</summary>
public sealed class JsonValueData : JsonData
{
    public static readonly JsonValueData Null = new("null");

    private JsonValueData(string json)
        : base(json)
    {
    }

    public JsonValueData(JsonValue? value)
        : base(Serialize(value))
    {
    }

    /// <summary>Returns a new, unshared value. JSON null gives a null reference.</summary>
    public JsonValue? ToValue() => JsonNode.Parse(Json) as JsonValue;

    public static bool TryFrom(JsonData data, out JsonValueData? result)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data is JsonValueData value)
        {
            result = value;
            return true;
        }

        JsonValueKind kind = KindOf(data.Json);
        result = kind is JsonValueKind.Object or JsonValueKind.Array ? null : new JsonValueData(data.Json);
        return result is not null;
    }
}
