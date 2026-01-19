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
    /// <summary>
    /// ARCHIVED: This component has been replaced by Create Multipart Form Body with Multipart Field components.
    /// Kept for backwards compatibility with existing Grasshopper definitions.
    /// </summary>
    [Obsolete("Use Create Multipart Form Body with Multipart Field components instead.")]
    public class CreateMultipartFormBodyNamed_ARCHIVED : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreatePostBody class.
        /// </summary>
        public CreateMultipartFormBodyNamed_ARCHIVED()
          : base("Create Multipart Form Body Named", "CMFBN",
              "[DEPRECATED] Create a Request Body that supports the multipart/form-data Content-Type with named fields",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Keys", "K", "Names of Multipart Form fields", GH_ParamAccess.list);
            pManager.AddParameter(new RequestBodyParam(), "Fields", "F", "Multipart form fields", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> keys = new List<string>();
            List<RequestBodyGoo> fields = new List<RequestBodyGoo>();

            DA.GetDataList(0, keys);
            DA.GetDataList(1, fields);

            if (keys.Count != fields.Count) throw new Exception("The number of Keys must match the number of Fields");
            RequestBodyMultipartForm form = new RequestBodyMultipartForm(keys, fields.Select(f => f.Value).ToList());

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
                return Properties.Resources.Icons_create_multipart_form_data_named_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("45321f4d-bb8e-4844-8b18-f663e3a95896"); }
        }
    }
}
