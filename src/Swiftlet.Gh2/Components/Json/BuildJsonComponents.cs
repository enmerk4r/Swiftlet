using System.Text.Json.Nodes;
using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Core.Json;
using Swiftlet.Gh2.Icons;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components.Json;

// "2. Request" tab: components that build or edit JSON. Inputs are immutable wrappers; every
// component edits its own freshly materialised node and wraps the result again.

[IoId(ComponentIds.CreateJsonArray)]
public sealed class CreateJsonArrayComponent : Component
{
    public CreateJsonArrayComponent()
        : base(new Nomen("Create JSON Array", "Combine a list of JTokens into a JArray", JsonComponentSupport.Tab, "2. Request")) { }

    public CreateJsonArrayComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs) =>
        inputs.Add(new JsonNodeParameter("JTokens", "Jt", "JTokens to be combined into an array", Access.Twig));

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new JsonArrayParameter("JArray", "Ja", "Resulting JArray"));

    protected override void Process(IDataAccess access)
    {
        access.GetTwig<JsonToken>(0, out var tokens);
        var result = new JsonArray();
        foreach (JsonToken? token in JsonComponentSupport.Leaves(tokens))
            result.Add(token?.ToNode()); // empty items become JSON null, as in GH1
        access.SetItem(0, new JsonArrayData(result));
    }
}

[IoId(ComponentIds.CreateJsonObject)]
public sealed class CreateJsonObjectComponent : Component
{
    public CreateJsonObjectComponent()
        : base(new Nomen("Create JSON Object", "Create a JObject from keys and values", JsonComponentSupport.Tab, "2. Request")) { }

    public CreateJsonObjectComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        // Optional lists need an explicit empty twig or the component never runs (see porting guide).
        inputs.AddText("Keys", "Ks", "List of JObject keys", Access.Twig, Requirement.MayBeMissing).Set(Array.Empty<string>());
        var values = new JsonNodeParameter("Values", "Vs", "List of JObject values", Access.Twig) { Requirement = Requirement.MayBeMissing };
        values.Set(Array.Empty<JsonToken>());
        inputs.Add(values);
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new JsonObjectParameter("JObject", "Jo", "Resulting JObject"));

    protected override void Process(IDataAccess access)
    {
        access.GetTwig<string>(0, out var keyTwig);
        access.GetTwig<JsonToken>(1, out var valueTwig);
        var keys = JsonComponentSupport.Leaves(keyTwig).ToList();
        var values = JsonComponentSupport.Leaves(valueTwig).ToList();
        // GH1 throws in both failure cases (mismatch, null/duplicate key), which leaves its output empty;
        // GH2 throws too so the output trees match (an AddError would leave a null item instead).
        if (keys.Count != values.Count)
            throw new InvalidOperationException("The number of keys must match the number of values");

        var result = new JsonObject();
        for (int i = 0; i < keys.Count; i++)
            result.Add(keys[i]!, values[i]?.ToNode());

        access.SetItem(0, new JsonObjectData(result));
    }
}

[IoId(ComponentIds.CreateJsonValue)]
public sealed class CreateJsonValueComponent : Component
{
    public CreateJsonValueComponent()
        : base(new Nomen("Create JSON Value", "Turn a Grasshopper value into a JSON value", JsonComponentSupport.Tab, "2. Request")) { }

    public CreateJsonValueComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs) =>
        inputs.AddGeneric("Value", "Vl", "A string, integer, number, DateTime or boolean. Leave empty for null.", Access.Item, Requirement.MayBeMissing);

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new JsonValueParameter("JValue", "Jv", "JSON Value"));

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out object input) || input is null)
        {
            access.SetItem(0, JsonValueData.Null);
            return;
        }

        JsonValue? value = JsonScalarText.CreateValue(input);
        if (value is null)
        {
            access.AddError("Unsupported value", $"Unable to create a JValue from object of type {input.GetType()}");
            return;
        }

        access.SetItem(0, new JsonValueData(value));
    }
}

[IoId(ComponentIds.MergeJsonObjects)]
public sealed class MergeJsonObjectsComponent : Component
{
    public MergeJsonObjectsComponent()
        : base(new Nomen("Merge JSON Objects",
            "Merge two JSON objects. Nested objects are merged recursively; conflicts use Mode: 0 = A wins, 1 = B wins, 2 = A wins unless null, 3 = B wins unless null.",
            JsonComponentSupport.Tab, "2. Request")) { }

    public MergeJsonObjectsComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new JsonObjectParameter("JSON Object A", "Ja", "Base JSON object."));
        inputs.Add(new JsonObjectParameter("JSON Object B", "Jb", "Incoming JSON object to merge into A."));
        inputs.AddInteger("Mode", "Md", "Conflict mode. 0 = A wins, 1 = B wins, 2 = A wins unless null, 3 = B wins unless null. Default is 1.",
            Access.Item, Requirement.MayBeMissing).Set((int)JsonObjectMerger.DefaultConflictMode);
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new JsonObjectParameter("JObject", "Jo", "Merged JObject."));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out JsonObjectData a);
        access.GetItem(1, out JsonObjectData b);
        if (!access.GetItem(2, out int rawMode))
            rawMode = (int)JsonObjectMerger.DefaultConflictMode;
        if (!JsonObjectMerger.TryParseConflictMode(rawMode, out JsonObjectMergeConflictMode mode))
            access.AddWarning("Invalid Mode", "Invalid Mode. Using default mode 1 (B wins).");

        access.SetItem(0, new JsonObjectData(JsonObjectMerger.Merge(a.ToObject(), b.ToObject(), mode)));
    }
}

[IoId(ComponentIds.RemoveJsonKey)]
public sealed class RemoveJsonKeyComponent : Component
{
    public RemoveJsonKeyComponent()
        : base(new Nomen("Remove JSON Key", "Remove a key from JObject", JsonComponentSupport.Tab, "2. Request")) { }

    public RemoveJsonKeyComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new JsonObjectParameter("JObject", "Jo", "JObject to remove the key from"));
        inputs.AddText("Key", "Ky", "Key to be removed");
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new JsonObjectParameter("JObject", "Jo", "Updated JObject"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out JsonObjectData obj);
        access.GetItem(1, out string key);
        JsonObject result = obj.ToObject();
        result.Remove(key); // absent keys are ignored, as in GH1
        access.SetItem(0, new JsonObjectData(result));
    }
}

[IoId(ComponentIds.SetJsonKey)]
public sealed class SetJsonKeyComponent : Component
{
    public SetJsonKeyComponent()
        : base(new Nomen("Set JSON Key", "Add or modify a JObject key", JsonComponentSupport.Tab, "2. Request")) { }

    public SetJsonKeyComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new JsonObjectParameter("JObject", "Jo", "JObject to add the JToken to"));
        inputs.AddText("Key", "Ky", "JToken key");
        inputs.Add(new JsonNodeParameter("JToken", "Jt", "JToken to be set on the JObject"));
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new JsonObjectParameter("JObject", "Jo", "Updated JObject"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out JsonObjectData obj);
        access.GetItem(1, out string key);
        access.GetItem(2, out JsonToken token);
        JsonObject result = obj.ToObject();
        result[key] = token.ToNode(); // add or replace, as in GH1
        access.SetItem(0, new JsonObjectData(result));
    }
}
