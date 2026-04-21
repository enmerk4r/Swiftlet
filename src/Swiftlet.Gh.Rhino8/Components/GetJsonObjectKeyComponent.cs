using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetJsonObjectKeyComponent : GH_Component
{
    public GetJsonObjectKeyComponent()
        : base("Get JSON Object Key", "GJOK", "Get a specific key from JObject", ShellNaming.Category, ShellNaming.ReadJson)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JObject", "JO", "JSON object to fetch the key from", GH_ParamAccess.item);
        pManager.AddTextParameter("Key", "K", "Key to fetch from JObject", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "JToken", "JT", "Fetched JToken", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        JsonObjectGoo? goo = null;
        string key = string.Empty;

        DA.GetData(0, ref goo);
        DA.GetData(1, ref key);

        if (goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to read JObject");
            return;
        }

        try
        {
            if (!goo.Value.TryGetPropertyValue(key, out JsonNode? token))
            {
                return;
            }

            DA.SetData(0, token is null ? JsonNodeGoo.CreateJsonNull() : new JsonNodeGoo(token));
        }
        catch (Exception exc)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, exc.Message);
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("b70e62d2-141b-48f9-b023-b0579178c22c");
}

