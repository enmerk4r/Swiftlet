using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateJsonArrayComponent : GH_Component
{
    public CreateJsonArrayComponent()
        : base("Create JSON Array", "CJA", "Combine a list of JTokens into a JArray", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "JTokens", "JT", "JTokens to be combined into an array", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonArrayParam(), "JArray", "JA", "Resulting JArray", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<JsonNodeGoo> tokens = [];
        DA.GetDataList(0, tokens);

        var result = new JsonArray();
        foreach (JsonNodeGoo? token in tokens)
        {
            result.Add(token?.Value is null ? null : JsonNodeCloner.Clone(token.Value));
        }

        DA.SetData(0, new JsonArrayGoo(result));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("BE0D2120-8576-44B1-934F-1A70568907A2");
}
