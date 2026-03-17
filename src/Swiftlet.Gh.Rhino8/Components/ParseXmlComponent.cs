using System.Xml;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ParseXmlComponent : GH_Component
{
    public ParseXmlComponent()
        : base("Parse XML", "XML", "Parse XML markup into a queryable XML document", ShellNaming.Category, ShellNaming.ReadXml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("XML", "X", "XML markup string", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new XmlNodeParam(), "Document", "D", "XML Document root node", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string xml = string.Empty;
        DA.GetData(0, ref xml);

        if (string.IsNullOrWhiteSpace(xml))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "XML input is empty");
            return;
        }

        try
        {
            var document = new XmlDocument();
            document.LoadXml(xml);
            DA.SetData(0, new XmlNodeGoo(document.DocumentElement));
        }
        catch (XmlException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"XML parsing error: {ex.Message}");
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("A1B2C3D4-E5F6-4A5B-9C0D-1E2F3A4B5C6D");
}

