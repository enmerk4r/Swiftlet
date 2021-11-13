using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class ApiKeyAuth : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ApiKeyAuth class.
        /// </summary>
        public ApiKeyAuth()
          : base("API Key", "API",
              "Create a header and a query param (you'll likely need one or the other, not both) for API key auth",
              NamingUtility.CATEGORY, NamingUtility.AUTH)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Key", "K", "Header key for your API auth", GH_ParamAccess.item);
            pManager.AddTextParameter("Value", "V", "Your API key value", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Your Auth Key-Value as an Http Header", GH_ParamAccess.item);
            pManager.AddParameter(new QueryParamParam(), "Query Param", "P", "Your Auth Key-Value as a URL query param", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string key = string.Empty;
            string value = string.Empty;

            DA.GetData(0, ref key);
            DA.GetData(1, ref value);

            HttpHeaderGoo hg = new HttpHeaderGoo(key, value);
            QueryParamGoo qg = new QueryParamGoo(key, value);

            DA.SetData(0, hg);
            DA.SetData(1, qg);
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
            get { return new Guid("09b3834f-0211-4339-a88e-738202f228fa"); }
        }
    }
}