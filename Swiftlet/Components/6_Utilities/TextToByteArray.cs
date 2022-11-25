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
    public class TextToByteArray : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public TextToByteArray()
          : base("Text To Byte Array", "TXTBA",
              "Converts text to a byte array",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

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
            pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Byte Array", GH_ParamAccess.item);
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
                return Properties.Resources.Icons_text_to_byte_array_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("52C16DF2-35B5-4315-BB02-4D0C2AA1DACF"); }
        }
    }
}