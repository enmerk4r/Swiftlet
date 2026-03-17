using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8;

internal static class JsonNodeConverter
{
    public static JsonNode? FromGrasshopperInput(object? input)
    {
        if (input is null)
        {
            return null;
        }

        if (input is JsonNode node)
        {
            return JsonNodeCloner.Clone(node);
        }

        if (input is GH_ObjectWrapper wrapper)
        {
            return FromGrasshopperInput(wrapper.Value);
        }

        return input switch
        {
            string text => ParseOrString(text),
            bool boolValue => JsonValue.Create(boolValue),
            int intValue => JsonValue.Create(intValue),
            long longValue => JsonValue.Create(longValue),
            double doubleValue => JsonValue.Create(doubleValue),
            float floatValue => JsonValue.Create(floatValue),
            decimal decimalValue => JsonValue.Create(decimalValue),
            _ => JsonValue.Create(Convert.ToString(input, CultureInfo.InvariantCulture)),
        };
    }

    private static JsonNode ParseOrString(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return JsonValue.Create(string.Empty)!;
        }

        try
        {
            return JsonNode.Parse(text) ?? JsonValue.Create(text)!;
        }
        catch (JsonException)
        {
            return JsonValue.Create(text)!;
        }
    }
}
