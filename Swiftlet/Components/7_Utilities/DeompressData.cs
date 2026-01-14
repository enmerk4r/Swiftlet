using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Commands;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class DeompressData : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CompressText class.
        /// </summary>
        public DeompressData()
          : base("Decompress Data", "UGZIP",
              "Un-GZIP byte array data",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ByteArrayParam(), "Compressed", "C", "Compressed text", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ByteArrayParam(), "Uncompressed", "U", "Uncompressed text", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ByteArrayGoo compressed = null;

            DA.GetData(0, ref compressed);

            var uncompressed = CompressionUtility.Decompress(compressed.Value);

            DA.SetData(0, new ByteArrayGoo(uncompressed));
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
                return Properties.Resources.Icons_decompress_data_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4da74eea-0134-4ab4-8e68-7e929e839bbe"); }
        }
    }
}