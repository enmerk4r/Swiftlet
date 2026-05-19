using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetHtmlAttributeValueComponent : GH_Component
{
    public GetHtmlAttributeValueComponent()
        : base("Get Attribute Value", "GATTR", "Get the value of an HTML attribute", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Node", "N", "HTML node", GH_ParamAccess.item);
        pManager.AddTextParameter("Attribute", "A", "Name of the HTML attribute", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Value", "V", "HTML Attribute value", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HtmlNodeGoo? goo = null;
        string attribute = string.Empty;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref attribute);

        if (goo?.Value is null || string.IsNullOrWhiteSpace(attribute))
        {
            return;
        }

        if (goo.Value.Attributes.Contains(attribute))
        {
            DA.SetData(0, goo.Value.Attributes[attribute].Value);
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("82D23920-743C-41B8-B5FF-085CB3F6F086");
}

