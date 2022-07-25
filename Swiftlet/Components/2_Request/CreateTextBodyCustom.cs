using System;
using System.Collections.Generic;
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
    public class CreateTextBodyCustom : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreatePostBody class.
        /// </summary>
        public CreateTextBodyCustom()
          : base("Create Text Body Custom", "CTBC",
              "Create a Request Body that supports text formats with a custom Content-Type header",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Content", "C", "Text contents of your request body", GH_ParamAccess.item);
            pManager.AddTextParameter("ContentType", "T", "Text contents of your request body", GH_ParamAccess.item, ContentTypeUtility.ApplicationJson);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
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
            object input = null;
            string txt = string.Empty;
            string contentType = string.Empty;

            DA.GetData(0, ref input);
            DA.GetData(1, ref contentType);

            if (input == null) { } // Do nothing
            else if (input is GH_String)
            {
                GH_String str = input as GH_String;
                if (str != null)
                {
                    txt = str.ToString();
                }
            }
            else if (input is JArrayGoo)
            {
                JArrayGoo arrayGoo = input as JArrayGoo;
                if (arrayGoo != null)
                {
                    txt = arrayGoo.Value.ToString();
                }
            }
            else if (input is JObjectGoo)
            {
                JObjectGoo objectGoo = input as JObjectGoo;
                if (objectGoo != null)
                {
                    txt = objectGoo.Value.ToString();
                }
            }
            else if (input is JTokenGoo)
            {
                JTokenGoo tokenGoo = input as JTokenGoo;
                if (tokenGoo != null)
                {
                    txt = tokenGoo.Value.ToString();
                }
            }
            else
            {
                throw new Exception(" Content must be a string, a JObject or a JArray");
            }

            RequestBodyText txtBody = new RequestBodyText(contentType, txt);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("70f48e9d-e37a-4695-961b-3b653542448d"); }
        }
    }
}