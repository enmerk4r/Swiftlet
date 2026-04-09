using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;

namespace Swiftlet.Gh.Rhino8.Params;

public sealed class XmlNodeParam : GH_Param<XmlNodeGoo>
{
    public XmlNodeParam()
        : base("XML Node", "XML", "Container for XML nodes", ShellNaming.Category, ShellNaming.ReadXml, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public override Guid ComponentGuid => new("3BC83348-4829-5CDC-BFFD-80DB24D0FBE2");

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());
}

