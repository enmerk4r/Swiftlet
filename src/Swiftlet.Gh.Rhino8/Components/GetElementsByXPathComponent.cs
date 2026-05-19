using Grasshopper.Kernel;
using HtmlAgilityPack;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetElementsByXPathComponent : GH_Component
{
    public GetElementsByXPathComponent()
        : base("Get Elements By XPATH", "BYXPATH", "Get HTML elements via an XPATH expression", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Parent", "P", "Parent node", GH_ParamAccess.item);
        pManager.AddTextParameter("XPATH", "X", "XPATH Expression", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Children", "C", "A list of child nodes", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HtmlNodeGoo? goo = null;
        string xpath = string.Empty;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref xpath);

        if (goo?.Value is null || string.IsNullOrWhiteSpace(xpath))
        {
            return;
        }

        HtmlNodeCollection? children = goo.Value.SelectNodes(xpath);
        if (children is null)
        {
            return;
        }

        DA.SetDataList(0, children.Select(static node => new HtmlNodeGoo(node)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("DCED6D9C-0654-484A-AEC3-02D941F22EA9");
}

