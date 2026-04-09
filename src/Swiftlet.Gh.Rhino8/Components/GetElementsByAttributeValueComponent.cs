using Grasshopper.Kernel;
using HtmlAgilityPack;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetElementsByAttributeValueComponent : GH_Component
{
    public GetElementsByAttributeValueComponent()
        : base("Get Elements By Attribute Value", "BYATTR", "Get all HTML elements where a certain attribute is equal to a certain value", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Parent", "P", "Parent node", GH_ParamAccess.item);
        pManager.AddTextParameter("Attribute", "A", "Name of the HTML attribute", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "Attribute value to search for", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Recursive", "R", "Determines whether to search for specified attribute in all of the descendants, or only one level down", GH_ParamAccess.item, true);
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Children", "C", "A list of child nodes", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HtmlNodeGoo? goo = null;
        string attribute = string.Empty;
        string value = string.Empty;
        bool recursive = true;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref attribute);
        DA.GetData(2, ref value);
        DA.GetData(3, ref recursive);

        if (goo?.Value is null || string.IsNullOrWhiteSpace(attribute))
        {
            return;
        }

        List<HtmlNodeGoo> matching = [];
        if (recursive)
        {
            HtmlNodeCollection? nodes = goo.Value.SelectNodes($".//*[@{attribute}='{value}']");
            if (nodes is not null)
            {
                matching.AddRange(nodes.Select(static node => new HtmlNodeGoo(node)));
            }
        }
        else
        {
            foreach (HtmlNode child in goo.Value.ChildNodes)
            {
                if (child.Attributes.Contains(attribute) && child.Attributes[attribute].Value == value)
                {
                    matching.Add(new HtmlNodeGoo(child));
                }
            }
        }

        DA.SetDataList(0, matching);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("560a0b7b-8c6a-4299-9433-6022657d178c");
}

