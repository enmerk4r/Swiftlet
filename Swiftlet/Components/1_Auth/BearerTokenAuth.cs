using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class BearerTokenAuth : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the BearerTokenAuth class.
        /// </summary>
        public BearerTokenAuth()
          : base("Bearer Token Auth", "BEARER",
              "Creates an Authorization header for your Bearer token",
              NamingUtility.CATEGORY, NamingUtility.AUTH)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Bearer Token", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Your Authorization heder", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string token = string.Empty;

            DA.GetData(0, ref token);
            DA.SetData(0, new HttpHeaderGoo("Authorization", $"Bearer {token}"));
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
                return Properties.Resources.Icons_token_auth_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("133b9c3f-63b0-42e2-bc8e-8eb7d4e916bc"); }
        }
    }
}