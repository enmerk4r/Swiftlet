using Grasshopper.Kernel;
using HtmlAgilityPack;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetElementsByClassNameComponent : GH_Component
{
    public GetElementsByClassNameComponent()
        : base("Get Elements By Class Name", "BYCLASS", "Get all HTML elements by a specific class name", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Parent", "P", "Parent node", GH_ParamAccess.item);
        pManager.AddTextParameter("Class", "C", "Name of the HTML class", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Recursive", "R", "Determines whether to search for specified attribute in all of the descendants, or only one level down", GH_ParamAccess.item, true);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Children", "C", "A list of child nodes", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HtmlNodeGoo? goo = null;
        string className = string.Empty;
        bool recursive = true;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref className);
        DA.GetData(2, ref recursive);

        if (goo?.Value is null || string.IsNullOrWhiteSpace(className))
        {
            return;
        }

        IEnumerable<HtmlNode> children = recursive ? goo.Value.Descendants() : goo.Value.ChildNodes;
        List<HtmlNodeGoo> matching = [];
        foreach (HtmlNode child in children)
        {
            if (!child.Attributes.Contains("class"))
            {
                continue;
            }

            string[] parts = child.Attributes["class"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Contains(className))
            {
                matching.Add(new HtmlNodeGoo(child));
            }
        }

        DA.SetDataList(0, matching);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("7D89F228-1FDD-4B38-81DA-ECE9C8634A74");
}

