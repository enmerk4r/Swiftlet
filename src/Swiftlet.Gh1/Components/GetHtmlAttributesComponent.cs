using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetHtmlAttributesComponent : GH_Component
{
    public GetHtmlAttributesComponent()
        : base("Get Html Attributes", "GATTRS", "Get all attributes and values of an HTML element", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Node", "N", "HTML node", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Names", "N", "List of HTML Attribute names", GH_ParamAccess.list);
        pManager.AddTextParameter("Values", "V", "List of HTML Attribute values", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HtmlNodeGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            return;
        }

        DA.SetDataList(0, goo.Value.Attributes.Select(static attr => attr.Name));
        DA.SetDataList(1, goo.Value.Attributes.Select(static attr => attr.Value));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("08D3C43F-1537-4C91-9CC0-D429FF62A89C");
}

