using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GenerateGUID : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public GenerateGUID()
          : base("Generate GUID", "GGUID",
              "Generates a random GUID (Globally Unique Identifier)\nThis is useful if you need a quick way to reliably generate unique IDs",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.senary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Generate", "G", "Generate a GUID!", GH_ParamAccess.item, true);
            pManager.AddTextParameter("Format", "F",
                "Format of the GUID as a single letter:\n" +

                "\nN - 32 digits:" +
                "\n00000000000000000000000000000000\n" +

                "\nD - 32 digits separated by hyphens:" +
                "\n00000000-0000-0000-0000-000000000000\n" +

                "\nB - 32 digits separated by hyphens, enclosed in braces:" +
                "\n{00000000-0000-0000-0000-000000000000}\n" +

                "\nP - 32 digits separated by hyphens, enclosed in parentheses:" +
                "\n(00000000-0000-0000-0000-000000000000)\n" +

                "\nX - Four hexadecimal values enclosed in braces, where the fourth value is a subset of eight hexadecimal values that is also enclosed in braces:" +
                "\n{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}\n",
                GH_ParamAccess.item,
                "D"
                );
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("GUID", "G", "Random GUID", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = true;
            string format = string.Empty;

            DA.GetData(0, ref run);
            DA.GetData(1, ref format);

            if (run)
            {
                Guid guid = Guid.NewGuid();

                DA.SetData(0, guid.ToString(format));
            }

            
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
                return Properties.Resources.Icons_generate_guid_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("e4cd167a-1d5b-4c43-a1db-70da520dbde7"); }
        }
    }
}