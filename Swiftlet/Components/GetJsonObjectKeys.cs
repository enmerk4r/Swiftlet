using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GetJsonObjectKeys : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetJsonObjectKeys class.
        /// </summary>
        public GetJsonObjectKeys()
          : base("Get JObject Keys", "KEYS",
              "Get all keys from JObject",
              NamingUtility.CATEGORY, NamingUtility.READ)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JTokenParam(), "JObject", "J", "JObject to get the keys from", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Keys", "K", "JObject keys", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            JTokenGoo goo = null;
            DA.GetData(0, ref goo);

            List<string> keys = new List<string>();
            JToken token = goo.Value;

            if (token is JObject)
            {
                JObject obj = token as JObject;
                keys = obj.Properties().Select(p => p.Name).ToList();
                DA.SetDataList(0, keys);
            }
            else
            {
                throw new Exception("Input is not a JObject");
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
            get { return new Guid("acc96ec4-b5d8-49fc-b8cf-2e604430388b"); }
        }
    }
}