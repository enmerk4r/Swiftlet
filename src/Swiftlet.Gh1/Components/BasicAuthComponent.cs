using System.Text;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class BasicAuthComponent : GH_Component
{
    public BasicAuthComponent()
        : base("Basic Auth", "BASIC", "Creates an Authorization header for your Basic auth", ShellNaming.Category, ShellNaming.Auth)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Username", "U", "Your Basic Auth username", GH_ParamAccess.item);
        pManager.AddTextParameter("Password", "P", "Your password. Keep in mind, you're in Grasshopper, so this is kinda sketchy... just sayin'", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Your Basic Auth header", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string userName = string.Empty;
        string password = string.Empty;

        DA.GetData(0, ref userName);
        DA.GetData(1, ref password);

        string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
        DA.SetData(0, new HttpHeaderGoo("Authorization", $"Basic {credentials}"));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("6D601BF4-8302-44B6-8D64-85818FB9A419");
}

