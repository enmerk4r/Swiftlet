using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetXmlElementsByTagComponent : GH_Component
{
    public GetXmlElementsByTagComponent()
        : base("Get XML Elements By Tag", "BYTAG", "Get XML elements by tag name", ShellNaming.Category, ShellNaming.ReadXml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Parent", "P", "Parent XML node", GH_ParamAccess.item);
        pManager.AddTextParameter("Tag", "T", "Tag name to search for", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Elements", "E", "Matching XML elements", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        XmlNodeGoo? goo = null;
        string tagName = string.Empty;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref tagName);

        if (goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Parent node is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(tagName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Tag name is empty");
            return;
        }

        XmlNodeList? elements = goo.Value is XmlElement element
            ? element.GetElementsByTagName(tagName)
            : goo.Value.SelectNodes($".//{tagName}");

        List<XmlNodeGoo> results = [];
        if (elements is not null)
        {
            foreach (XmlNode child in elements)
            {
                results.Add(new XmlNodeGoo(child));
            }
        }

        DA.SetDataList(0, results);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B2C3D4E5-F6A7-5B6C-0D1E-2F3A4B5C6D7E");
}

