using System.Text.Json.Nodes;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateJsonValueComponent : GH_Component
{
    public CreateJsonValueComponent()
        : base("Create JSON Value", "CJV", "Turn a Grasshopper value into a JSON value", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Value", "V", "A string, integer, number, DateTime or boolean. Leave empty for null.", GH_ParamAccess.item);
        pManager[0].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new JsonValueParam(), "JValue", "JV", "JSON Value", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        object input = null;
        DA.GetData(0, ref input);

        if (input is null)
        {
            DA.SetData(0, JsonValueGoo.CreateJsonNull());
            return;
        }

        JsonValue? value = CreateValue(Unwrap(input));
        if (value is null)
        {
            throw new Exception($"Unable to create a JValue from object of type {input.GetType()}");
        }

        DA.SetData(0, new JsonValueGoo(value));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("CA82E229-FCDB-415F-BD0B-5F37C1EF8C3F");

    private static object? Unwrap(object input)
    {
        return input is GH_ObjectWrapper wrapper ? wrapper.Value : input;
    }

    private static JsonValue? CreateValue(object? input)
    {
        // Grasshopper goo unwrap to their CLR values; the conversion itself is shared with GH2.
        object? value = input switch
        {
            GH_String goo => goo.Value,
            GH_Integer goo => goo.Value,
            GH_Number goo => goo.Value,
            GH_Boolean goo => goo.Value,
            GH_Time goo => goo.Value,
            _ => input,
        };
        return Swiftlet.Runtime.JsonScalarText.CreateValue(value);
    }
}
