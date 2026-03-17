using System.Text.Json;
using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class StringifyJsonTokenComponent : GH_Component
{
    public StringifyJsonTokenComponent()
        : base("Stringify JSON Token", "SJT", "Convert any abstract JToken to an indented JSON string", ShellNaming.Category, ShellNaming.ReadJson)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "JToken", "JT", "JToken to be converted to a string", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "JSON String", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        JsonNodeGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to read JSON Token");
            return;
        }

        if (goo.RepresentsJsonNull)
        {
            DA.SetData(0, null);
            return;
        }

        if (goo.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to read JSON Token");
            return;
        }

        DA.SetData(0, Stringify(goo.Value));
    }

    private static string? Stringify(JsonNode node)
    {
        return node switch
        {
            JsonValue value when value.TryGetValue<string>(out string? stringValue) => stringValue,
            JsonValue value when value.TryGetValue<bool>(out bool boolValue) => boolValue.ToString(),
            JsonValue value when value.TryGetValue<double>(out double numberValue) => numberValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonValue value when value.TryGetValue<long>(out long longValue) => longValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonValue value when value.TryGetValue<int>(out int intValue) => intValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonValue value when value.TryGetValue<decimal>(out decimal decimalValue) => decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonValue value => value.ToString(),
            _ => node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
        };
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("5c90048b-9a14-43b5-9996-c67511880604");
}

