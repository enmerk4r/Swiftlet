using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class DeconstructQueryParam : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DeconstructHttpResponse class.
        /// </summary>
        public DeconstructQueryParam()
          : base("Deconstruct Query Param", "DQP",
              "Deconstruct a Query Param into its constituent parts",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new QueryParamParam(), "Param", "P", "Query Param to deconstruct", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Key", "K", "Query Parameter Key", GH_ParamAccess.item);
            pManager.AddTextParameter("Value", "V", "Query Parameter Value", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            QueryParamGoo query = null;
            DA.GetData(0, ref query);

            if (query == null) return;

            DA.SetData(0, query.Value.Key);
            DA.SetData(1, query.Value.Value);
           

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
                return Properties.Resources.Icons_deconstruct_query_param_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4a7f8985-730a-45da-803d-d5c21ef1ea0e"); }
        }
    }
}