using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Util;

namespace Swiftlet.Components._6_Save
{
    public class SaveCSV : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SaveCSV class.
        /// </summary>
        public SaveCSV()
          : base("Save CSV", "CSV",
              "Save formatted CSV Lines as a CSV file",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Lines", "L", "CSV-formatted lines (use the \"Create CSV Line\" component)", GH_ParamAccess.list);
            pManager.AddTextParameter("Path", "P", "Output path", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Bytes", "B", "Size of saved file in bytes", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> lines = new List<string>();
            string path = string.Empty;

            DA.GetDataList(0, lines);
            DA.GetData(1, ref path);

            using (StreamWriter writer = new StreamWriter(path))
            {
                foreach(string line in lines)
                {
                    writer.WriteLine(line);
                }
            }

            long length = new System.IO.FileInfo(path).Length;

            DA.SetData(0, length);
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
                return Properties.Resources.Icons_save_csv_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C8F5887A-30F1-4FE5-A737-DAE37BDACC5C"); }
        }
    }
}