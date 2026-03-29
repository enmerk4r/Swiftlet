using Grasshopper.Kernel;
using Swiftlet.Core.Json;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class MergeJsonObjectsComponent : GH_Component
{
    public MergeJsonObjectsComponent()
        : base(
            "Merge JSON Objects",
            "MJO",
            "Merge two JSON objects. Nested objects are merged recursively; conflicts use Mode: 0 = A wins, 1 = B wins, 2 = A wins unless null, 3 = B wins unless null.",
            ShellNaming.Category,
            ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.octonary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JSON Object A", "A", "Base JSON object.", GH_ParamAccess.item);
        pManager.AddParameter(new JsonObjectParam(), "JSON Object B", "B", "Incoming JSON object to merge into A.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Mode", "M", "Conflict mode. 0 = A wins, 1 = B wins, 2 = A wins unless null, 3 = B wins unless null. Default is 1.", GH_ParamAccess.item, (int)JsonObjectMerger.DefaultConflictMode);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonObjectParam(), "JObject", "JO", "Merged JObject.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        JsonObjectGoo? objectAGoo = null;
        JsonObjectGoo? objectBGoo = null;
        int rawMode = (int)JsonObjectMerger.DefaultConflictMode;

        DA.GetData(0, ref objectAGoo);
        DA.GetData(1, ref objectBGoo);
        DA.GetData(2, ref rawMode);

        if (objectAGoo?.Value is null || objectBGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to read one or both input JSON objects.");
            return;
        }

        if (!JsonObjectMerger.TryParseConflictMode(rawMode, out JsonObjectMergeConflictMode mode))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid Mode. Using default mode 1 (B wins).");
        }

        DA.SetData(0, new JsonObjectGoo(JsonObjectMerger.Merge(objectAGoo.Value, objectBGoo.Value, mode)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("D0FD4B4D-EC77-4471-8087-31B0B6446A01");
}
