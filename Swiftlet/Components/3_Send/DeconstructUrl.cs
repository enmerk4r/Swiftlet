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
    public class DeconstructUrl : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DeconstructHttpResponse class.
        /// </summary>
        public DeconstructUrl()
          : base("Deconstruct URL", "DURL",
              "Deconstruct a URL into its constituent parts",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "A URL string to be deconstructed", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Base", "B", "Base URL", GH_ParamAccess.item);
            pManager.AddTextParameter("Scheme", "S", "URL scheme (http or https)", GH_ParamAccess.item);
            pManager.AddTextParameter("Host", "H", "Host component of the URL", GH_ParamAccess.item);
            pManager.AddTextParameter("Route", "R", "A route (path) to the online resource", GH_ParamAccess.item);
            pManager.AddParameter(new QueryParamParam(), "Params", "P", "Http Query Parameters", GH_ParamAccess.list);


        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string url = string.Empty;
            DA.GetData(0, ref url);

            if (!url.StartsWith("http")) throw new Exception(" A valid URL must include a scheme (http or https)");

            Uri myUri = new Uri(url);
            NameValueCollection parameters = HttpUtility.ParseQueryString(myUri.Query);

            List<QueryParamGoo> paramGoo = new List<QueryParamGoo>();

            string baseUri = url.Split('?').First();

            foreach (string key in parameters.AllKeys)
            {
                string value = parameters.Get(key);
                paramGoo.Add(new QueryParamGoo(key, value));
            }

            DA.SetData(0, baseUri);
            DA.SetData(1, myUri.Scheme);
            DA.SetData(2, myUri.Host);
            DA.SetData(3, myUri.AbsolutePath);
            DA.SetDataList(4, paramGoo);

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
                return Properties.Resources.Icons_deconstruct_url_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6240a6fa-f6fa-47af-a796-0a2fc3dd6cc1"); }
        }
    }
}