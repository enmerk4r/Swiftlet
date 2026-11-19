using System.Xml;
using System.Xml.XPath;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetXmlElementsByXPathComponent : GH_Component
{
    public GetXmlElementsByXPathComponent()
        : base("Get XML Elements By XPath", "BYXPATH", "Query XML elements using an XPath expression", ShellNaming.Category, ShellNaming.ReadXml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Parent", "P", "Parent XML node", GH_ParamAccess.item);
        pManager.AddTextParameter("XPath", "X", "XPath expression", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Elements", "E", "Matching XML elements", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        XmlNodeGoo? goo = null;
        string xpath = string.Empty;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref xpath);

        if (goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Parent node is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(xpath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "XPath expression is empty");
            return;
        }

        try
        {
            XmlNodeList? elements = goo.Value.SelectNodes(xpath);
            if (elements is null)
            {
                return;
            }

            DA.SetDataList(0, elements.Cast<XmlNode>().Select(static node => new XmlNodeGoo(node)));
        }
        catch (XPathException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Invalid XPath: {ex.Message}");
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("C3D4E5F6-A7B8-6C7D-1E2F-3A4B5C6D7E8F");
}

