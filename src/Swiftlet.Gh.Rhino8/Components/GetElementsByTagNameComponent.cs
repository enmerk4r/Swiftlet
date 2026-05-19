using Grasshopper.Kernel;
using HtmlAgilityPack;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetElementsByTagNameComponent : GH_Component
{
    public GetElementsByTagNameComponent()
        : base("Get Elements By Tag Name", "BYTAG", "Get HTML Elements by Tag Name", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Parent", "P", "Parent node", GH_ParamAccess.item);
        pManager.AddTextParameter("Tag", "T", "Name of the HTML tag", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Recursive", "R", "Determines whether to search for specified tags in all of the descendants, or only one level down", GH_ParamAccess.item, true);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Children", "C", "A list of child nodes", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HtmlNodeGoo? goo = null;
        string tag = string.Empty;
        bool recursive = true;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref tag);
        DA.GetData(2, ref recursive);

        if (goo?.Value is null || string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        IEnumerable<HtmlNode> nodes = recursive
            ? goo.Value.Descendants(tag)
            : goo.Value.ChildNodes.Where(static node => node.Name is not null).Where(node => node.Name == tag);

        DA.SetDataList(0, nodes.Select(static node => new HtmlNodeGoo(node)));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("42EA89C9-46BD-4C81-8553-E0FD11CB652A");
}

