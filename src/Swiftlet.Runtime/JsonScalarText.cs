using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Swiftlet.Runtime;

/// <summary>
/// Text/number conversions of JSON nodes shared by the GH1 and GH2 JSON components
/// (moved verbatim from the GH1 Read JSON Value, Stringify JSON Token and Create JSON Value components).
/// </summary>
internal static class JsonScalarText
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>Read JSON Value "AsNumber": double, long, int or decimal JSON numbers.</summary>
    public static bool TryGetDouble(JsonValue value, out double number)
    {
        if (value.TryGetValue<double>(out number))
        {
            return true;
        }

        if (value.TryGetValue<long>(out long longValue))
        {
            number = longValue;
            return true;
        }

        if (value.TryGetValue<int>(out int intValue))
        {
            number = intValue;
            return true;
        }

        if (value.TryGetValue<decimal>(out decimal decimalValue))
        {
            number = (double)decimalValue;
            return true;
        }

        number = default;
        return false;
    }

    /// <summary>Read JSON Value "AsString": strings unquoted, booleans as True/False, numbers invariant, containers indented.</summary>
    public static string? AsText(JsonNode node)
    {
        return node switch
        {
            JsonValue value when value.TryGetValue<string>(out string? stringValue) => stringValue,
            JsonValue value when value.TryGetValue<bool>(out bool boolValue) => boolValue.ToString(),
            JsonValue value when TryGetDouble(value, out double numberValue) => numberValue.ToString(CultureInfo.InvariantCulture),
            JsonValue value => value.ToString(),
            _ => node.ToJsonString(Indented),
        };
    }

    /// <summary>Stringify JSON Token: like <see cref="AsText"/> but with Stringify's own numeric probing order.</summary>
    public static string? Stringify(JsonNode node)
    {
        return node switch
        {
            JsonValue value when value.TryGetValue<string>(out string? stringValue) => stringValue,
            JsonValue value when value.TryGetValue<bool>(out bool boolValue) => boolValue.ToString(),
            JsonValue value when value.TryGetValue<double>(out double numberValue) => numberValue.ToString(CultureInfo.InvariantCulture),
            JsonValue value when value.TryGetValue<long>(out long longValue) => longValue.ToString(CultureInfo.InvariantCulture),
            JsonValue value when value.TryGetValue<int>(out int intValue) => intValue.ToString(CultureInfo.InvariantCulture),
            JsonValue value when value.TryGetValue<decimal>(out decimal decimalValue) => decimalValue.ToString(CultureInfo.InvariantCulture),
            JsonValue value => value.ToString(),
            _ => node.ToJsonString(Indented),
        };
    }

    /// <summary>Create JSON Value from a plain CLR value; null when the type is not supported.</summary>
    public static JsonValue? CreateValue(object? input)
    {
        return input switch
        {
            null => null,
            string value => JsonValue.Create(value),
            int value => JsonValue.Create(value),
            long value => JsonValue.Create(value),
            double value => JsonValue.Create(value),
            float value => JsonValue.Create(value),
            decimal value => JsonValue.Create(value),
            bool value => JsonValue.Create(value),
            DateTime value => JsonValue.Create(value),
            DateTimeOffset value => JsonValue.Create(value),
            _ => null,
        };
    }
}
