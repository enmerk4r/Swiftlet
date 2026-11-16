using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateTextBodyCustomComponent : GH_Component
{
    public CreateTextBodyCustomComponent()
        : base("Create Text Body Custom", "CTBC", "Create a Request Body that supports text formats with a custom Content-Type header", ShellNaming.Category, ShellNaming.Request)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Content", "C", "Text contents of your request body", GH_ParamAccess.item);
        pManager.AddTextParameter("ContentType", "T", "Text contents of your request body", GH_ParamAccess.item, ContentTypes.ApplicationJson);

        pManager[0].Optional = true;
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        object input = null;
        string contentType = ContentTypes.ApplicationJson;

        DA.GetData(0, ref input);
        DA.GetData(1, ref contentType);

        var body = new RequestBodyText(contentType, BodyInputConverter.ToLegacyText(input));
        DA.SetData(0, new RequestBodyGoo(body));
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("70F48E9D-E37A-4695-961B-3B653542448D");
}

