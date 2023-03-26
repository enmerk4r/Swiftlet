using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Microsoft.VisualBasic.FileIO;
using Rhino.Geometry;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class SplitTextIntoLines : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateCsvLine class.
        /// </summary>
        public SplitTextIntoLines()
          : base("Split Text Into Lines", "STIL",
              "Takes a block of text and splits it into individual lines by the newline character",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Text to split into lines", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Lines", "L", "Individual lines", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string text = string.Empty;
            DA.GetData(0, ref text);

            List<string> lines = text.Split('\n').ToList();

            DA.SetDataList(0, lines);
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
            get { return new Guid("3561ef51-707a-414b-b344-68b79108bc46"); }
        }
    }
}