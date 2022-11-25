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
    public class TextToBase64 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public TextToBase64()
          : base("Text To Base64", "TB64",
              "Converts text to a Base64 encoded string",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Input Text", GH_ParamAccess.item);
            pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");
            
            pManager[1].Optional = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Base64", "B", "Base64 encoded string", GH_ParamAccess.item);
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

            byte[] array = null;

            string upperEncoding = encoding.ToUpper();

            switch (upperEncoding)
            {
                case "ASCII":
                    array = Encoding.ASCII.GetBytes(txt); break;
                case "UNICODE":
                    array = Encoding.Unicode.GetBytes(txt); break;
                case "UTF8":
                    array = Encoding.UTF8.GetBytes(txt); break;
                case "UTF7":
                    array = Encoding.UTF7.GetBytes(txt); break;
                case "UTF32":
                    array = Encoding.UTF32.GetBytes(txt); break;
                default:
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{encoding} is an unknown encoding"); return;
            }

            string base64 = System.Convert.ToBase64String(array);

            DA.SetData(0, base64);
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
                return Properties.Resources.Icons_text_to_base64_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ac216550-bea2-4f2d-b9fa-c5b0df90c423"); }
        }
    }
}