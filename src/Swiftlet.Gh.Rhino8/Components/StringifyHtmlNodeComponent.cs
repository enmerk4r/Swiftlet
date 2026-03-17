using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class StringifyHtmlNodeComponent : GH_Component
{
    public StringifyHtmlNodeComponent()
        : base("Stringify HTML Node", "SHTML", "Convert an HTML Node to cleartext", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "HTML Node", "N", "HTML Node object", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "HTML Markup as text", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HtmlNodeGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            return;
        }

        DA.SetData(0, goo.Value.OuterHtml);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("48819932-F530-4764-B9FA-FB44998CF11F");
}

