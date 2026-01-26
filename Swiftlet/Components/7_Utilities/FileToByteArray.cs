using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class FileToByteArray : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public FileToByteArray()
          : base("File To Byte Array", "FBA",
              "Read a file into a byte array",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Input File", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Byte Array", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = string.Empty;
            DA.GetData(0, ref path);

            byte[] array = File.ReadAllBytes(path);

            DA.SetData(0, new ByteArrayGoo(array));
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
                return Properties.Resources.Icons_file_to_byte_array_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6514b6d2-779e-4cd6-92b7-91a2f4a36360"); }
        }
    }
}