using System.Text.Json;
using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ParseJsonStringComponent : GH_Component
{
    public ParseJsonStringComponent()
        : base("Parse JSON String", "PJS", "Parse a string into a searchable JSON Object", ShellNaming.Category, ShellNaming.ReadJson)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "JSON-formatted string to be parsed", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "JToken", "JT", "Parsed JSON Token", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string json = string.Empty;
        DA.GetData(0, ref json);

        try
        {
            JsonNode? node = JsonNode.Parse(json);
            if (node is null)
            {
                if (string.Equals(json?.Trim(), "null", StringComparison.OrdinalIgnoreCase))
                {
                    DA.SetData(0, JsonNodeGoo.CreateJsonNull());
                    return;
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to parse JSON string");
                return;
            }

            DA.SetData(0, new JsonNodeGoo(node));
        }
        catch (JsonException)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to parse JSON string");
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("18b4fd6e-e6b9-4958-b8c5-1e0cf5a3d016");
}

