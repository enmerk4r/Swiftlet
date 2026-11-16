using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class RemoveJsonKeyComponent : GH_Component
{
    public RemoveJsonKeyComponent()
        : base("Remove JSON Key", "RJK", "Remove a key from JObject", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.octonary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JObject", "JO", "JObject to remove the key from", GH_ParamAccess.item);
        pManager.AddTextParameter("Key", "K", "Key to be removed", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JObject", "JO", "Updated JObject", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        JsonObjectGoo? objectGoo = null;
        string key = string.Empty;

        DA.GetData(0, ref objectGoo);
        DA.GetData(1, ref key);
        JsonObject result = JsonNodeCloner.CloneObject(objectGoo.Value);
        try
        {
            result.Remove(key);
        }
        catch
        {
        }

        DA.SetData(0, new JsonObjectGoo(result));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("452CF748-8D55-42F8-B141-499B8F931891");
}
