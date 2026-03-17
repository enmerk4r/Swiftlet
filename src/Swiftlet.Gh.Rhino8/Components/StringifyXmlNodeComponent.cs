using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class StringifyXmlNodeComponent : GH_Component
{
    public StringifyXmlNodeComponent()
        : base("Stringify XML Node", "XML2STR", "Convert an XML node back to a formatted XML string", ShellNaming.Category, ShellNaming.ReadXml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Node", "N", "XML node to stringify", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Indent", "I", "Pretty-print with indentation", GH_ParamAccess.item, true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("XML", "X", "XML string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        XmlNodeGoo? goo = null;
        bool indent = true;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref indent);

        if (goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node is null");
            return;
        }

        DA.SetData(0, goo.ToXmlString(indent));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B8C9D0E1-F2A3-1122-6D7E-8F9001122334");
}

