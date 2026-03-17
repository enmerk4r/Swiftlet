using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class HtmlNodeParam : GH_Param<HtmlNodeGoo>
{
    public HtmlNodeParam()
        : base("HTML Node", "HTML", "Container for HTML nodes", ShellNaming.Category, ShellNaming.ReadHtml, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid => new("2AC72237-3718-4BCB-AEFC-79CA13C9FAD1");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

