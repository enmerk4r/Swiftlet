using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DeconstructHeaderComponent : GH_Component
{
    public DeconstructHeaderComponent()
        : base("Deconstruct Http Header", "DHH", "Deconstruct a Header into its constituent parts", ShellNaming.Category, ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Header to deconstruct", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "Query Parameter Key", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "Query Parameter Value", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HttpHeaderGoo? header = null;
        if (!DA.GetData(0, ref header) || header?.Value is null)
        {
            return;
        }

        DA.SetData(0, header.Value.Key);
        DA.SetData(1, header.Value.Value);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("4c0bdeaa-e4c2-4c7d-b139-358c23e39a96");
}

