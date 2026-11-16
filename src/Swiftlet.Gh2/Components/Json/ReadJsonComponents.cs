using System.Text.Json;
using System.Text.Json.Nodes;
using Grasshopper2.Components;
using Grasshopper2.Data;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Json;
using Swiftlet.Gh2.Icons;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components.Json;

// "4. Read JSON" tab. JSON travels as the immutable Swiftlet.Core.Json wrappers; each component
// materialises a private node (ToNode/ToObject/...) so nothing mutable is shared between threads.

/// <summary>Shared helpers for the JSON components.</summary>
internal static class JsonComponentSupport
{
    public const string Tab = "Swiftlet";

    /// <summary>Every leaf of a twig in order, with null for empty slots (GH1 lists keep nulls).</summary>
    public static IEnumerable<T?> Leaves<T>(Twig<T>? twig) where T : class
    {
        if (twig is null)
            yield break;
        for (int i = 0; i < twig.LeafCount; i++)
            yield return twig.NullAt(i) ? null : twig.ItemAt(i);
    }
}

[IoId(ComponentIds.ParseJsonString)]
public sealed class ParseJsonStringComponent : Component
{
    public ParseJsonStringComponent()
        : base(new Nomen("Parse JSON String", "Parse a string into a searchable JSON Object", JsonComponentSupport.Tab, "4. Read JSON")) { }

    public ParseJsonStringComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs) => inputs.AddText("Text", "Tx", "JSON-formatted string to be parsed");

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new JsonNodeParameter("JToken", "Jt", "Parsed JSON Token"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string text);
        try
        {
            // "null" parses to JSON null (JsonToken.Null), as in GH1.
            access.SetItem(0, JsonToken.Parse(text));
        }
        catch (JsonException ex)
        {
            access.AddError("Unable to parse JSON string", ex.Message);
        }
    }
}

[IoId(ComponentIds.GetJsonObjectKey)]
public sealed class GetJsonObjectKeyComponent : Component
{
    public GetJsonObjectKeyComponent()
        : base(new Nomen("Get JSON Object Key", "Get a specific key from JObject", JsonComponentSupport.Tab, "4. Read JSON")) { }

    public GetJsonObjectKeyComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new JsonObjectParameter("JObject", "Jo", "JSON object to fetch the key from"));
        inputs.AddText("Key", "Ky", "Key to fetch from JObject");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new JsonNodeParameter("JToken", "Jt", "Fetched JToken"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out JsonObjectData obj);
        access.GetItem(1, out string key);
        // A missing key produces no output, as in GH1; a key holding JSON null yields JSON null.
        if (obj.ToObject().TryGetPropertyValue(key, out JsonNode? token))
            access.SetItem(0, new JsonToken(token));
    }
}

[IoId(ComponentIds.ReadJsonArray)]
public sealed class ReadJsonArrayComponent : Component
{
    public ReadJsonArrayComponent()
        : base(new Nomen("Read JSON Array", "Read a JSON Array into a series of searchable JSON objects", JsonComponentSupport.Tab, "4. Read JSON")) { }

    public ReadJsonArrayComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs) =>
        inputs.Add(new JsonNodeParameter("Array", "Ja", "JSON Array to be broken down into individual JSON objects"));

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new JsonNodeParameter("JTokens", "Jt", "JSON tokens as list", Access.Twig));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out JsonToken token);
        if (token.ToNode() is not JsonArray array)
        {
            access.AddWarning("Input token is not an array", "Read JSON Array expects a JSON array.");
            return;
        }

        access.SetTwig(0, array.Select(item => new JsonToken(item)).ToArray());
    }
}

[IoId(ComponentIds.ReadJsonObject)]
public sealed class ReadJsonObjectComponent : Component
{
    public ReadJsonObjectComponent()
        : base(new Nomen("Read JSON Object", "Get all keys and values from JObject", JsonComponentSupport.Tab, "4. Read JSON")) { }

    public ReadJsonObjectComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs) =>
        inputs.Add(new JsonNodeParameter("JObject", "Jo", "JObject to get the keys and values from"));

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Keys", "Ks", "JObject keys", Access.Twig);
        outputs.Add(new JsonNodeParameter("JTokens", "Jt", "Parsed JSON Tokens", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out JsonToken token);
        // GH1 throws here (empty outputs); match it.
        if (token.ToNode() is not JsonObject obj)
            throw new InvalidOperationException("Input is not a JObject");

        var keys = new List<string>();
        var values = new List<JsonToken>();
        foreach (KeyValuePair<string, JsonNode?> pair in obj)
        {
            keys.Add(pair.Key);
            values.Add(new JsonToken(pair.Value));
        }

        access.SetTwig(0, keys.ToArray());
        access.SetTwig(1, values.ToArray());
    }
}

[IoId(ComponentIds.ReadJsonValue)]
public sealed class ReadJsonValueComponent : Component
{
    public ReadJsonValueComponent()
        : base(new Nomen("Read JSON Value", "Read JSON Value", JsonComponentSupport.Tab, "4. Read JSON")) { }

    public ReadJsonValueComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs) =>
        inputs.Add(new JsonNodeParameter("JValue", "Jv", "JSON Value to be read"));

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("AsString", "As", "JSON value as string");
        outputs.AddNumber("AsNumber", "An", "JSON value as number");
        outputs.AddBoolean("AsBool", "Ab", "JSON value as boolean");
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out JsonToken token);
        JsonNode? node = token.ToNode();
        if (node is null)
        {
            // JSON null: a null text item and nothing else, as in GH1.
            access.SetItem(0, (string?)null);
            return;
        }

        access.SetItem(0, JsonScalarText.AsText(node));
        if (node is JsonValue numberValue && JsonScalarText.TryGetDouble(numberValue, out double number))
            access.SetItem(1, number);
        if (node is JsonValue boolValue && boolValue.TryGetValue(out bool flag))
            access.SetItem(2, flag);
    }
}

[IoId(ComponentIds.SeparateJsonTokens)]
public sealed class SeparateJsonTokensComponent : Component
{
    public SeparateJsonTokensComponent()
        : base(new Nomen("Separate JSON Tokens", "Separate tokens into JObjects, JArrays and JValues", JsonComponentSupport.Tab, "4. Read JSON")) { }

    public SeparateJsonTokensComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs) =>
        inputs.Add(new JsonNodeParameter("JTokens", "Jt", "JTokens to be separated", Access.Twig));

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new JsonObjectParameter("JObjects", "Jo", "Separated JObjects", Access.Twig));
        outputs.Add(new JsonArrayParameter("JArrays", "Ja", "Separated JArrays", Access.Twig));
        outputs.Add(new JsonValueParameter("JValues", "Jv", "Separated JValues", Access.Twig));
    }

    protected override void Process(IDataAccess access)
    {
        access.GetTwig<JsonToken>(0, out var tokens);
        var objects = new List<JsonObjectData>();
        var arrays = new List<JsonArrayData>();
        var values = new List<JsonValueData>();
        foreach (JsonToken? token in JsonComponentSupport.Leaves(tokens))
        {
            if (token is null)
            {
                access.AddWarning("Failed to read JToken", "The list contains an empty item.");
                return;
            }

            if (JsonObjectData.TryFrom(token, out var obj))
                objects.Add(obj!);
            else if (JsonArrayData.TryFrom(token, out var array))
                arrays.Add(array!);
            else if (JsonValueData.TryFrom(token, out var value))
                values.Add(value!); // includes JSON null
        }

        access.SetTwig(0, objects.ToArray());
        access.SetTwig(1, arrays.ToArray());
        access.SetTwig(2, values.ToArray());
    }
}

[IoId(ComponentIds.StringifyJsonToken)]
public sealed class StringifyJsonTokenComponent : Component
{
    public StringifyJsonTokenComponent()
        : base(new Nomen("Stringify JSON Token", "Convert any abstract JToken to an indented JSON string", JsonComponentSupport.Tab, "4. Read JSON")) { }

    public StringifyJsonTokenComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs) =>
        inputs.Add(new JsonNodeParameter("JToken", "Jt", "JToken to be converted to a string"));

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Text", "Tx", "JSON String");

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out JsonToken token);
        JsonNode? node = token.ToNode();
        access.SetItem(0, node is null ? null : JsonScalarText.Stringify(node));
    }
}
