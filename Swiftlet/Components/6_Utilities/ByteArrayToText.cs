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
    public class ByteArrayToText : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public ByteArrayToText()
          : base("Byte Array To Text", "BATXT",
              "Converts a byte array to text",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ByteArrayParam(), "Array", "A", "Input Byte Array", GH_ParamAccess.item);
            pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");

            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter( "Text", "T", "Output text", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ByteArrayGoo goo = null;
            string encoding = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref encoding);

            string txt = string.Empty;

            string upperEncoding = encoding.ToUpper();

            switch (upperEncoding)
            {
                case "ASCII":
                    txt = Encoding.ASCII.GetString(goo.Value); break;
                case "UNICODE":
                    txt = Encoding.Unicode.GetString(goo.Value); break;
                case "UTF8":
                    txt = Encoding.UTF8.GetString(goo.Value); break;
                case "UTF7":
                    txt = Encoding.UTF7.GetString(goo.Value); break;
                case "UTF32":
                    txt = Encoding.UTF32.GetString(goo.Value); break;
                default:
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{encoding} is an unknown encoding"); return;
            }

            DA.SetData(0, txt);
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
            get { return new Guid("15e4454a-ee5b-483f-886f-f10307421ff7"); }
        }
    }
}