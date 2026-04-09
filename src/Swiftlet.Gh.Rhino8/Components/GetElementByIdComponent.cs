using Grasshopper.Kernel;
using HtmlAgilityPack;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class GetElementByIdComponent : GH_Component
{
    public GetElementByIdComponent()
        : base("Get Element By Id", "BYID", "Get an HTML element by ID", ShellNaming.Category, ShellNaming.ReadHtml)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Parent", "P", "Parent node", GH_ParamAccess.item);
        pManager.AddTextParameter("ID", "I", "HTML Element ID", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new HtmlNodeParam(), "Element", "E", "Found element", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HtmlNodeGoo? goo = null;
        string elementId = string.Empty;
        DA.GetData(0, ref goo);
        DA.GetData(1, ref elementId);

        if (goo?.Value is null || string.IsNullOrWhiteSpace(elementId))
        {
            return;
        }

        var tempDocument = new HtmlDocument();
        tempDocument.LoadHtml(goo.Value.OuterHtml);
        HtmlNode? element = tempDocument.GetElementbyId(elementId);
        if (element is not null)
        {
            DA.SetData(0, new HtmlNodeGoo(element));
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("799427C4-0E1B-4ADB-B3A0-1CE33DDF7A41");
}

