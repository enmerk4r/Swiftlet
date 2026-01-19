using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateHttpHeader : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateHttpHeader class.
        /// </summary>
        public CreateHttpHeader()
          : base("Create Http Header", "CH",
              "Create a new Http Header",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.septenary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Key", "K", "Header Key", GH_ParamAccess.item);
            pManager.AddTextParameter("Value", "V", "Header Value", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new HttpHeaderParam(), "Header", "H", "Http Header", GH_ParamAccess.item);
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

            DA.SetData(0, new HttpHeaderGoo(key, value));
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
                return Properties.Resources.Icons_create_http_header_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7dc20210-c08f-466f-af5e-286f70f4c630"); }
        }
    }
}