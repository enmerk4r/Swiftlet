using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using Grasshopper2.Types.Conversion;
using GrasshopperIO;
using Swiftlet.Core.Json;

namespace Swiftlet.Gh2.Types;

// JSON travels through GH2 as immutable Swiftlet.Core.Json.JsonData values (compact JSON text).
// Components call ToNode()/ToObject()/ToArray()/ToValue() to get their own mutable copy.

public sealed class JsonTokenTypeAssistant : TypeAssistant<JsonToken>
{
    public JsonTokenTypeAssistant() : base("JSON Token") { }

    public override JsonToken Copy(JsonToken instance) => instance;

    public override string DescribePrimary(Pear<JsonToken> pear) => pear.Item.ToString();

    public override string Format(JsonToken instance) => instance.ToString();

    public override bool Same(JsonToken a, JsonToken b) => a.Equals(b);

    public override bool Write(IWriter writer, Name location, JsonToken instance)
    {
        writer.String(location, instance.Json);
        return true;
    }

    public override bool Read(IReader reader, Name location, out JsonToken instance)
    {
        instance = null!;
        if (!reader.HasItem(location))
            return false;

        instance = JsonToken.Parse(reader.String(location));
        return true;
    }
}

public sealed class JsonObjectTypeAssistant : TypeAssistant<JsonObjectData>
{
    public JsonObjectTypeAssistant() : base("JSON Object") { }

    public override JsonObjectData Copy(JsonObjectData instance) => instance;

    public override string DescribePrimary(Pear<JsonObjectData> pear) => pear.Item.ToString();

    public override string Format(JsonObjectData instance) => instance.ToString();

    public override bool Same(JsonObjectData a, JsonObjectData b) => a.Equals(b);

    public override bool Write(IWriter writer, Name location, JsonObjectData instance)
    {
        writer.String(location, instance.Json);
        return true;
    }

    public override bool Read(IReader reader, Name location, out JsonObjectData instance)
    {
        instance = null!;
        return reader.HasItem(location)
            && JsonObjectData.TryFrom(JsonToken.Parse(reader.String(location)), out instance!);
    }
}

public sealed class JsonArrayTypeAssistant : TypeAssistant<JsonArrayData>
{
    public JsonArrayTypeAssistant() : base("JSON Array") { }

    public override JsonArrayData Copy(JsonArrayData instance) => instance;

    public override string DescribePrimary(Pear<JsonArrayData> pear) => pear.Item.ToString();

    public override string Format(JsonArrayData instance) => instance.ToString();

    public override bool Same(JsonArrayData a, JsonArrayData b) => a.Equals(b);

    public override bool Write(IWriter writer, Name location, JsonArrayData instance)
    {
        writer.String(location, instance.Json);
        return true;
    }

    public override bool Read(IReader reader, Name location, out JsonArrayData instance)
    {
        instance = null!;
        return reader.HasItem(location)
            && JsonArrayData.TryFrom(JsonToken.Parse(reader.String(location)), out instance!);
    }
}

public sealed class JsonValueTypeAssistant : TypeAssistant<JsonValueData>
{
    public JsonValueTypeAssistant() : base("JSON Value") { }

    public override JsonValueData Copy(JsonValueData instance) => instance;

    public override string DescribePrimary(Pear<JsonValueData> pear) => pear.Item.ToString();

    public override string Format(JsonValueData instance) => instance.ToString();

    public override bool Same(JsonValueData a, JsonValueData b) => a.Equals(b);

    public override bool Write(IWriter writer, Name location, JsonValueData instance)
    {
        writer.String(location, instance.Json);
        return true;
    }

    public override bool Read(IReader reader, Name location, out JsonValueData instance)
    {
        instance = null!;
        return reader.HasItem(location)
            && JsonValueData.TryFrom(JsonToken.Parse(reader.String(location)), out instance!);
    }
}

/// <summary>
/// Conversions between the JSON types, mirroring the casts the Grasshopper 1 goo types allow:
/// anything widens to a token; a token narrows only when it holds the matching kind.
/// </summary>
public sealed class JsonConversions : ConversionRepository
{
    [Merit(Merit.Direct)]
    public static void Convert(JsonObjectData source, out JsonToken target) => target = JsonToken.From(source);

    [Merit(Merit.Direct)]
    public static void Convert(JsonArrayData source, out JsonToken target) => target = JsonToken.From(source);

    [Merit(Merit.Direct)]
    public static void Convert(JsonValueData source, out JsonToken target) => target = JsonToken.From(source);

    [Merit(Merit.Direct)]
    public static void Convert(JsonToken source, out JsonObjectData target)
    {
        if (!JsonObjectData.TryFrom(source, out JsonObjectData? result))
            throw new InvalidCastException("The JSON token is not an object.");

        target = result!;
    }

    [Merit(Merit.Direct)]
    public static void Convert(JsonToken source, out JsonArrayData target)
    {
        if (!JsonArrayData.TryFrom(source, out JsonArrayData? result))
            throw new InvalidCastException("The JSON token is not an array.");

        target = result!;
    }

    [Merit(Merit.Direct)]
    public static void Convert(JsonToken source, out JsonValueData target)
    {
        if (!JsonValueData.TryFrom(source, out JsonValueData? result))
            throw new InvalidCastException("The JSON token is an object or array, not a value.");

        target = result!;
    }
}
