using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class SeparateJsonTokensComponent : GH_Component
{
    public SeparateJsonTokensComponent()
        : base("Separate JSON Tokens", "SJT", "Separate tokens into JObjects, JArrays and JValues", ShellNaming.Category, ShellNaming.ReadJson)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "JTokens", "JT", "JTokens to be separated", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JObjects", "JO", "Separated JObjects", GH_ParamAccess.list);
        pManager.AddParameter(new JsonArrayParam(), "JArrays", "JA", "Separated JArrays", GH_ParamAccess.list);
        pManager.AddParameter(new JsonValueParam(), "JValues", "JV", "Separated JValues", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<JsonNodeGoo> tokens = [];
        DA.GetDataList(0, tokens);

        List<JsonObjectGoo> objects = [];
        List<JsonArrayGoo> arrays = [];
        List<JsonValueGoo> values = [];

        foreach (JsonNodeGoo goo in tokens)
        {
            if (goo is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to read JToken");
                return;
            }

            if (goo.RepresentsJsonNull)
            {
                values.Add(JsonValueGoo.CreateJsonNull());
            }
            else if (goo.Value is JsonObject obj)
            {
                objects.Add(new JsonObjectGoo(obj));
            }
            else if (goo.Value is JsonArray array)
            {
                arrays.Add(new JsonArrayGoo(array));
            }
            else if (goo.Value is JsonValue value)
            {
                values.Add(new JsonValueGoo(value));
            }
        }

        DA.SetDataList(0, objects);
        DA.SetDataList(1, arrays);
        DA.SetDataList(2, values);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("034d6e3b-ae02-4d7b-995b-88f752f36790");
}

