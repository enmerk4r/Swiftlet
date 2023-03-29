using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class Base64ToText : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public Base64ToText()
          : base("Base64 to Text", "TB64",
              "Converts a Base64 encoded string to cleartext",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Base64", "B", "Base64 encoded string", GH_ParamAccess.item);
            pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");
            
            pManager[1].Optional = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Converted Text", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string txt = string.Empty;
            string encoding = string.Empty;

            DA.GetData(0, ref txt);
            DA.GetData(1, ref encoding);

            string outputString = string.Empty;
            byte[] base64DecodedBytes = System.Convert.FromBase64String(txt);

            string upperEncoding = encoding.ToUpper();

            switch (upperEncoding)
            {
                case "ASCII":
                    outputString = Encoding.ASCII.GetString(base64DecodedBytes); break;
                case "UNICODE":
                    outputString = Encoding.Unicode.GetString(base64DecodedBytes); break;
                case "UTF8":
                    outputString = Encoding.UTF8.GetString(base64DecodedBytes); break;
                case "UTF7":
                    outputString = Encoding.UTF7.GetString(base64DecodedBytes); break;
                case "UTF32":
                    outputString = Encoding.UTF32.GetString(base64DecodedBytes); break;
                default:
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{encoding} is an unknown encoding"); return;
            }

            DA.SetData(0, outputString);
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
                return Properties.Resources.Icons_base64_to_text_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2385c560-3f23-4196-bb17-5a0c8f7b4aca"); }
        }
    }
}