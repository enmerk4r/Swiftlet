using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;
using Swiftlet.Imaging;
using System.Drawing;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class BitmapToMeshComponent : GH_Component
{
    public BitmapToMeshComponent()
        : base("Bitmap to Mesh", "BTM", "Converts a bitmap to a Rhino mesh", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.septenary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Input Bitmap", GH_ParamAccess.item);
        pManager.AddRectangleParameter("Rectangle", "R", "Optional boundary", GH_ParamAccess.item);
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddMeshParameter("Mesh", "M", "Output colored mesh", GH_ParamAccess.item);
        pManager.AddColourParameter("Colors", "C", "Bitmap colors (in the original order)", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Width", "W", "Bitmap width (in pixels)", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Height", "H", "Bitmap height (in pixels)", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        BitmapGoo? goo = null;
        Rectangle3d rect = default;

        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            return;
        }

        SwiftletImage image = goo.Value;
        Mesh mesh = DA.GetData(1, ref rect)
            ? Mesh.CreateFromPlane(rect.Plane, rect.X, rect.Y, image.Width - 1, image.Height - 1)
            : Mesh.CreateFromPlane(Plane.WorldXY, new Interval(0, image.Width), new Interval(0, image.Height), image.Width - 1, image.Height - 1);

        var rows = new List<List<Color>>();
        for (int y = image.Height - 1; y >= 0; y--)
        {
            var row = new List<Color>();
            for (int x = 0; x < image.Width; x++)
            {
                SwiftletColor pixel = image.GetPixel(x, y);
                var color = Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
                mesh.VertexColors.Add(color);
                row.Add(color);
            }

            rows.Add(row);
        }

        rows.Reverse();
        var colors = new List<Color>(image.Width * image.Height);
        foreach (List<Color> row in rows)
        {
            colors.AddRange(row);
        }

        DA.SetData(0, mesh);
        DA.SetDataList(1, colors);
        DA.SetData(2, image.Width);
        DA.SetData(3, image.Height);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("DAE3641E-A624-4E2F-9AAD-B278301F84A1");
}

