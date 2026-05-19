using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ReadJsonObjectComponent : GH_Component
{
    public ReadJsonObjectComponent()
        : base("Read JSON Object", "RJO", "Get all keys and values from JObject", ShellNaming.Category, ShellNaming.ReadJson)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "JObject", "JO", "JObject to get the keys and values from", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Keys", "K", "JObject keys", GH_ParamAccess.list);
        pManager.AddParameter(new JsonNodeParam(), "JTokens", "JT", "Parsed JSON Tokens", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        JsonNodeGoo? goo = null;
        DA.GetData(0, ref goo);

        if (goo is null)
        {
            return;
        }

        JsonNode token = goo.Value;
        if (token is JsonObject obj)
        {
            List<string> keys = [];
            List<JsonNodeGoo> tokens = [];

            foreach (KeyValuePair<string, JsonNode?> pair in obj)
            {
                keys.Add(pair.Key);
                tokens.Add(new JsonNodeGoo(pair.Value));
            }

            DA.SetDataList(0, keys);
            DA.SetDataList(1, tokens);
        }
        else
        {
            throw new Exception("Input is not a JObject");
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("acc96ec4-b5d8-49fc-b8cf-2e604430388b");
}

