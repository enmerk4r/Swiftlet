using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class SetJsonKeyComponent : GH_Component
{
    public SetJsonKeyComponent()
        : base("Set JSON Key", "SJK", "Add or modify a JObject key", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.octonary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JObject", "JO", "JObject to add the JToken to", GH_ParamAccess.item);
        pManager.AddTextParameter("Key", "K", "JToken key", GH_ParamAccess.item);
        pManager.AddParameter(new JsonNodeParam(), "JToken", "JT", "JToken to be set on the JObject", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JObject", "JO", "Updated JObject", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        JsonObjectGoo? objectGoo = null;
        string key = string.Empty;
        JsonNodeGoo? tokenGoo = null;

        DA.GetData(0, ref objectGoo);
        DA.GetData(1, ref key);
        DA.GetData(2, ref tokenGoo);

        JsonObject result = JsonNodeCloner.CloneObject(objectGoo.Value);
        JsonNode? token = tokenGoo?.Value is null ? null : JsonNodeCloner.Clone(tokenGoo.Value);

        try
        {
            result.Add(key, token);
        }
        catch
        {
            result[key] = token;
        }

        DA.SetData(0, new JsonObjectGoo(result));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("306EE7F8-1C8E-4500-A3B9-8215B8934B43");
}
