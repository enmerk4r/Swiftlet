using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateCsvLine : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateCsvLine class.
        /// </summary>
        public CreateCsvLine()
          : base("Create CSV Line", "CSVL",
              "Formats multiple ",
              NamingUtility.CATEGORY, NamingUtility.SAVE_TO_DISK)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Cells", "C", "Cell values of a CSV line", GH_ParamAccess.list);
            pManager.AddTextParameter("Delimiter", "D", "CSV delimeter", GH_ParamAccess.item, ",");

            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Line", "L", "Formatted CSV line", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> cells = new List<string>();
            string delimiter = ",";
            DA.GetDataList(0, cells);
            DA.GetData(1, ref delimiter);

            string line = string.Empty;

            foreach(string cell in cells)
            {
                string cleanCell = cell;
                if (cell.Contains(delimiter))
                {
                    cleanCell = $"\"{cell}\"";
                }
                line += $"{cleanCell}{delimiter}";
            }

            line = line.Remove(line.Length - 1, 1);
            DA.SetData(0, line);
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
                return Properties.Resources.Icons_create_csv_line_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D9B2041E-FF4E-446E-B634-4EBACD47051E"); }
        }
    }
}