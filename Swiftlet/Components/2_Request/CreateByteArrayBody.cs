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
    public class CreateByteArrayBody : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreatePostBody class.
        /// </summary>
        public CreateByteArrayBody()
          : base("Create Byte Array Body", "CBAB",
              "Create a Request Body that supports Byte Array content",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Input byte array", GH_ParamAccess.item);
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
            ByteArrayGoo goo = null;
            string contentType = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref contentType);


            RequestBodyByteArray txtBody = new RequestBodyByteArray(contentType, goo.Value);
            RequestBodyGoo body = new RequestBodyGoo(txtBody);

            DA.SetData(0, body);
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
            get { return new Guid("43f12a67-6ab6-450d-8a10-63081f20cdbf"); }
        }
    }
}