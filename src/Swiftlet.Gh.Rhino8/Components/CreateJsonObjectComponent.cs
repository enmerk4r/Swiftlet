using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateJsonObjectComponent : GH_Component
{
    public CreateJsonObjectComponent()
        : base("Create JSON Object", "CJO", "Create a JObject from keys and values", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Keys", "K", "List of JObject keys", GH_ParamAccess.list);
        pManager.AddParameter(new JsonNodeParam(), "Values", "V", "List of JObject values", GH_ParamAccess.list);
        pManager[0].Optional = true;
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JObject", "O", "Resulting JObject", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<string> keys = [];
        List<JsonNodeGoo> values = [];

        DA.GetDataList(0, keys);
        DA.GetDataList(1, values);

        if (keys.Count != values.Count)
        {
            throw new Exception("The number of keys must match the number of values");
        }

        var result = new JsonObject();
        for (int index = 0; index < keys.Count; index++)
        {
            string key = keys[index];
            JsonNode? value = values[index]?.Value is null ? null : JsonNodeCloner.Clone(values[index].Value);
            result.Add(key, value);
        }

        DA.SetData(0, new JsonObjectGoo(result));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("2892726B-FB0E-424A-877D-353F8AC18DC5");
}
