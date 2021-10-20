using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class ParseJsonObject : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ParseJsonObject class.
        /// </summary>
        public ParseJsonObject()
          : base("Get JSON Key", "KEY",
              "Search for a JSON key",
              NamingUtility.CATEGORY, NamingUtility.READ)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JTokenParam(), "JSON", "J", "JSON Object to search for the key in", GH_ParamAccess.item);
            pManager.AddTextParameter("Key", "K", "Key to search for", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JTokenParam(), "Value", "V", "Value as a JSON token", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            JTokenGoo goo = null;
            string str = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref str);

            JToken token = goo.Value;
            if (token is JObject)
            {
                JObject obj = token as JObject;
                JToken found = obj.GetValue(str);

                if (found != null) DA.SetData(0, new JTokenGoo(found));
            }
            else if (token is JArray)
            {
                throw new Exception("JToken is a JArray, not a JObject");
            }
            else
            {
                throw new Exception("JToken is not a JObject");
            }

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
            get { return new Guid("f3931057-ebad-4a07-9c03-5bde0cd107cf"); }
        }
    }
}