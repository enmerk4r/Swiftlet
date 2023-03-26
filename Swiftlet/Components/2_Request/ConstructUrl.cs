using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateUrl : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DeconstructHttpResponse class.
        /// </summary>
        public CreateUrl()
          : base("Create URL", "CURL",
              "Construct a URL from its constituent parts",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Scheme", "S", "URL scheme (http or https)", GH_ParamAccess.item);
            pManager.AddTextParameter("Host", "H", "Host component of the URL", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Port", "P", "TCP port number", GH_ParamAccess.item);
            pManager.AddTextParameter("Route", "R", "Components of a route to an online resource", GH_ParamAccess.list);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "Constructed URL string", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string scheme = string.Empty;
            string host = string.Empty;
            int port = -1;
            List<string> routeComponents = new List<string>();
            

            DA.GetData(0, ref scheme);
            DA.GetData(1, ref host);
            DA.GetData(2, ref port);
            DA.GetDataList(3, routeComponents);

            UriBuilder builder = new UriBuilder();
            builder.Scheme = scheme;
            builder.Host = host;

            if (port > 0) builder.Port = port;

            List<string> pathTokens = new List<string>();

            foreach (string component in routeComponents)
            {
                pathTokens.AddRange(component.Split('/'));
            }

            string path = "";

            foreach (string component in pathTokens) 
            {
                string f = $"/{component}";

                if (f != "/")
                {
                    path += f;
                }
            };

            

            if (!string.IsNullOrEmpty(path))
            {
                path = path.Substring(1, path.Length - 1);
                builder.Path = path;
            }

            DA.SetData(0, builder.ToString());
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
                return Properties.Resources.Icons_construct_url_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("104e0b1e-9954-475c-8046-a7259a8967f5"); }
        }
    }
}