using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.ModelBinding;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class BitmapToMesh : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public BitmapToMesh()
          : base("Bitmap to Mesh", "BTM",
              "Converts a bitmap to a Rhino mesh",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.septenary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Input Bitmap", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Rectangle", "R", "Optional boundary", GH_ParamAccess.item);

            pManager[1].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Bitmap", "B", "Output bitmap", GH_ParamAccess.item);
            pManager.AddColourParameter("Colors", "C", "Bitmap colors (in the original order)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Width", "W", "Bitmap width (in pixels)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Height", "H", "Bitmap height (in pixels)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            BitmapGoo goo = null;
            Rectangle3d rect = default(Rectangle3d);

            DA.GetData(0, ref goo);

            Bitmap bmp = goo.Value;
            Mesh mesh;

            if (DA.GetData(1, ref rect))
            {
                mesh = Mesh.CreateFromPlane(rect.Plane, rect.X, rect.Y, bmp.Width - 1, bmp.Height - 1);
            }
            else
            {
                Interval xInt = new Interval(0, bmp.Width);
                Interval yInt = new Interval(0, bmp.Height);

                mesh = Mesh.CreateFromPlane(Plane.WorldXY, xInt, yInt, bmp.Width - 1, bmp.Height - 1);
            }

            List<List<Color>> rows = new List<List<Color>>();

            for (int y = bmp.Height - 1; y >= 0 ; y--)
            {
                List<Color> row = new List<Color>();
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    mesh.VertexColors.Add(c);
                    row.Add(c);
                }
                rows.Add(row);
            }

            List<Color> colors = new List<Color>();
            rows.Reverse();

            foreach (List<Color> row in rows)
            {
                colors.AddRange(row);
            }


            DA.SetData(0, mesh);
            DA.SetDataList(1, colors);
            DA.SetData(2, bmp.Width);
            DA.SetData(3, bmp.Height);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.Icons_byte_array_to_mesh_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("dae3641e-a624-4e2f-9aad-b278301f84a1"); } 
        }
    }
}