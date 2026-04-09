using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class BearerTokenAuthComponent : GH_Component
{
    public BearerTokenAuthComponent()
        : base("Bearer Token Auth", "BEARER", "Creates an Authorization header for your Bearer token", ShellNaming.Category, ShellNaming.Auth)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Token", "T", "Bearer Token", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Your Authorization heder", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string token = string.Empty;
        DA.GetData(0, ref token);
        DA.SetData(0, new HttpHeaderGoo("Authorization", $"Bearer {token}"));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("133B9C3F-63B0-42E2-BC8E-8EB7D4E916BC");
}

