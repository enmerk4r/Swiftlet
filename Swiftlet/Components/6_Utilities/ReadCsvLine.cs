using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Microsoft.VisualBasic.FileIO;
using Rhino.Geometry;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class ReadCsvLine : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateCsvLine class.
        /// </summary>
        public ReadCsvLine()
          : base("Read CSV Line", "RCSVL",
              "Extracts individual values from a delimeter-separated line",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Line", "L", "Formatted CSV line", GH_ParamAccess.item);
            pManager.AddTextParameter("Delimiter", "D", "CSV delimeter", GH_ParamAccess.item, ",");

            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Cells", "C", "Cell values of a CSV line", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string line = string.Empty;
            string delimiter = ",";

            DA.GetData(0, ref line);
            DA.GetData(1, ref delimiter);

            TextFieldParser parser = new TextFieldParser(new StringReader(line));

            if (line.Contains("\""))
            {
                parser.HasFieldsEnclosedInQuotes = true;
            }
            parser.SetDelimiters(delimiter);

            List<string> cells = new List<string>();

            while (!parser.EndOfData)
            {
                cells.AddRange(parser.ReadFields());
            }

            DA.SetDataList(0, cells);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0478d672-bfbd-4502-b627-07b2e5d7f664"); }
        }
    }
}