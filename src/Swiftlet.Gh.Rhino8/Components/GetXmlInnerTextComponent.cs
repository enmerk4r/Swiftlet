using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetXmlInnerTextComponent : GH_Component
{
    public GetXmlInnerTextComponent()
        : base("Get XML Inner Text", "XMLTEXT", "Get the inner text content of an XML element", ShellNaming.Category, ShellNaming.ReadXml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Element", "E", "XML element", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("InnerText", "T", "Inner text content", GH_ParamAccess.item);
        pManager.AddTextParameter("InnerXml", "X", "Inner XML markup", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        XmlNodeGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element is null");
            return;
        }

        DA.SetData(0, goo.Value.InnerText);
        DA.SetData(1, goo.Value.InnerXml);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("F6A7B8C9-D0E1-9F00-4B5C-6D7E8F900112");
}

