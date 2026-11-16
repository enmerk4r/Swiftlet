using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetXmlChildNodesComponent : GH_Component
{
    public GetXmlChildNodesComponent()
        : base("Get XML Child Nodes", "XMLCHILDREN", "Get the child nodes of an XML element", ShellNaming.Category, ShellNaming.ReadXml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Parent", "P", "Parent XML element", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Children", "C", "Child nodes", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        XmlNodeGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Parent is null");
            return;
        }

        List<XmlNodeGoo> children = [];
        foreach (XmlNode child in goo.Value.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element || (child.NodeType == XmlNodeType.Text && !string.IsNullOrWhiteSpace(child.Value)))
            {
                children.Add(new XmlNodeGoo(child));
            }
        }

        DA.SetDataList(0, children);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("A7B8C9D0-E1F2-0011-5C6D-7E8F90011223");
}

