using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CreateMultipartFormBodyUnnamed : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreatePostBody class.
        /// </summary>
        public CreateMultipartFormBodyUnnamed()
          : base("Create Multipart Form Body Unnamed", "CMFBU",
              "Create a Request Body that supports the multipart/form-data Content-Type with unnamed fields",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new RequestBodyParam(), "Fields", "F", "Multipart form fields", GH_ParamAccess.list);
            pManager[0].Optional = true;
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
            List<RequestBodyGoo> fields = new List<RequestBodyGoo>();

            DA.GetDataList(0, fields);

            RequestBodyMultipartForm form = new RequestBodyMultipartForm(fields.Select(f => f.Value).ToList());

            DA.SetData(0, new RequestBodyGoo(form));
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
            get { return new Guid("bfe253aa-ab8a-4424-872a-37f5cb1e7096"); }
        }
    }
}