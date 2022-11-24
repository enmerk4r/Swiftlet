using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateByteArrayBodyFromFile : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreatePostBody class.
        /// </summary>
        public CreateByteArrayBodyFromFile()
          : base("Create Byte Array Body from File", "BABFF",
              "Create a Request Body that supports Byte Array content by providing a filepath",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Path to file", GH_ParamAccess.item);
            pManager.AddTextParameter("ContentType", "T", "Text contents of your request body", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = string.Empty;
            string contentType = string.Empty;

            DA.GetData(0, ref path);
            DA.GetData(1, ref contentType);

            var content = File.ReadAllBytes(path);

            RequestBodyByteArray txtBody = new RequestBodyByteArray(contentType, content);
            RequestBodyGoo goo = new RequestBodyGoo(txtBody);

            DA.SetData(0, goo);
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
                return Properties.Resources.Icons_create_byte_array_body_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("05df5f52-6e60-4492-9332-03189a83ec18"); }
        }
    }
}