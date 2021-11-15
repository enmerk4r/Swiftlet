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
    public class BasicAuth : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the StandardAuth class.
        /// </summary>
        public BasicAuth()
          : base("Basic Auth", "BASIC",
              "Creates an Authorization header for your Basic auth",
              NamingUtility.CATEGORY, NamingUtility.AUTH)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Username", "U", "Your Basic Auth username", GH_ParamAccess.item);
            pManager.AddTextParameter("Password", "P", "Your password. Keep in mind, you're in Grasshopper, so this is kinda sketchy... just sayin'", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Your Basic Auth header", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string uname = string.Empty;
            string pwd = string.Empty;

            DA.GetData(0, ref uname);
            DA.GetData(1, ref pwd);

            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{uname}:{pwd}"));

            DA.SetData(0, new HttpHeaderGoo("Authorization", $"Basic {credentials}"));
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
                return Properties.Resources.Icons_basic_auth_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6d601bf4-8302-44b6-8d64-85818fb9a419"); }
        }
    }
}