using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ReadJsonArrayComponent : GH_Component
{
    public ReadJsonArrayComponent()
        : base("Read JSON Array", "RJA", "Read a JSON Array into a series of searchable JSON objects", ShellNaming.Category, ShellNaming.ReadJson)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "Array", "JA", "JSON Array to be broken down into individual JSON objects", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "JTokens", "JT", "JSON tokens as list", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        JsonNodeGoo? goo = null;
        DA.GetData(0, ref goo);

        if (goo is not null)
        {
            JsonNode token = goo.Value;
            if (token is JsonArray array)
            {
                DA.SetDataList(0, array.Select(static o => new JsonNodeGoo(o)));
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input token is not an array");
                return;
            }
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("66bfbf37-4512-4f12-85b5-90003e224fb0");
}

