using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ReadJsonValueComponent : GH_Component
{
    public ReadJsonValueComponent()
        : base("Read JSON Value", "RJV", "Read JSON Value", ShellNaming.Category, ShellNaming.ReadJson)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new JsonNodeParam(), "JValue", "JV", "JSON Value to be read", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("AsString", "S", "JSON value as string", GH_ParamAccess.item);
        pManager.AddNumberParameter("AsNumber", "N", "JSON value as number", GH_ParamAccess.item);
        pManager.AddBooleanParameter("AsBool", "B", "JSON value as boolean", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        JsonNodeGoo? goo = null;
        DA.GetData(0, ref goo);

        if (goo is null)
        {
            return;
        }

        JsonNode inputToken = goo.Value;

        try
        {
            if (inputToken is null)
            {
                DA.SetData(0, null);
            }
            else
            {
                DA.SetData(0, Swiftlet.Runtime.JsonScalarText.AsText(inputToken));
            }
        }
        catch
        {
        }

        try
        {
            if (inputToken is JsonValue numberValue && Swiftlet.Runtime.JsonScalarText.TryGetDouble(numberValue, out double number))
            {
                DA.SetData(1, number);
            }
        }
        catch
        {
        }

        try
        {
            if (inputToken is JsonValue boolValue && boolValue.TryGetValue<bool>(out bool value))
            {
                DA.SetData(2, value);
            }
        }
        catch
        {
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("d59d2f8a-23d2-4e84-9a80-1ac245ea57e5");
}

