using System.Text.Json;
using System.Text.Json.Nodes;
using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class FormatJsonStringComponent : GH_Component
{
    public FormatJsonStringComponent()
        : base("Format Json String", "FJS", "Prettify a JSON string by formatting it with proper indentations", ShellNaming.Category, ShellNaming.ReadJson)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("JSON String", "J", "Unformatted JSON string", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Pretty JSON", "P", "Formatted JSON string (this helps with readability)", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string json = string.Empty;
        DA.GetData(0, ref json);

        try
        {
            JsonNode? token = JsonNode.Parse(json);
            if (token is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to parse JSON string");
                return;
            }

            DA.SetData(0, token.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("37c912a2-2ab5-4926-8911-cb220b6adb25");
}

