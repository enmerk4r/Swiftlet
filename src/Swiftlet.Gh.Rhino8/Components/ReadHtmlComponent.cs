using Grasshopper.Kernel;
using HtmlAgilityPack;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ReadHtmlComponent : GH_Component
{
    public ReadHtmlComponent()
        : base("Read HTML", "HTML", "Read HTML Markup", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "HTML Markup", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Node", "N", "Html Node", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string html = string.Empty;
        DA.GetData(0, ref html);

        var document = new HtmlDocument();
        document.LoadHtml(html);
        DA.SetData(0, new HtmlNodeGoo(document.DocumentNode));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("349B64D0-63E5-4263-85E1-788D03A71920");
}

