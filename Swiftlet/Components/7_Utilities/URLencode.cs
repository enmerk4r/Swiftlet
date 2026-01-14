using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class URLencode : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public URLencode()
          : base("URL encode", "URLE",
              "URL-encodes a string to make it URL-safe",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Text to be URL encoded", GH_ParamAccess.item);
            pManager.AddTextParameter("Encoding", "E", "Can be ASCII, Unicode, UTF8, UTF7, UTF32", GH_ParamAccess.item, "UTF8");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Encoded", "E", "URL-encoded string", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string txt = string.Empty;
            string encodingStr = "UTF8";

            DA.GetData(0, ref txt);
            DA.GetData(1, ref encodingStr);

            Encoding encoding;
            string upperEncoding = encodingStr.ToUpper();

            switch (upperEncoding)
            {
                case "ASCII":
                    encoding = Encoding.ASCII; break;
                case "UNICODE":
                    encoding = Encoding.Unicode; break;
                case "UTF8":
                    encoding = Encoding.UTF8; break;
                case "UTF7":
                    encoding = Encoding.UTF7; break;
                case "UTF32":
                    encoding = Encoding.UTF32; break;
                default:
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{encodingStr} is an unknown encoding"); return;
            }

            DA.SetData(0, HttpUtility.UrlEncode(txt, encoding));
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
                return Properties.Resources.Icons_url_encode_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9d385c97-7975-43a3-9ae7-2fabbe5bd4c8"); }
        }
    }
}