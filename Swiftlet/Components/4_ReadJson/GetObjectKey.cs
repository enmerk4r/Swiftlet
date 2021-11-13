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
    public class GetObjectKey : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetObjectKey class.
        /// </summary>
        public GetObjectKey()
          : base("Get JSON Object Key", "GJOK",
              "Get a specific key from JObject",
              NamingUtility.CATEGORY, NamingUtility.READ_JSON)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JObjectParam(), "JObject", "JO", "JObject to fetch the key from", GH_ParamAccess.item);
            pManager.AddTextParameter("Key", "K", "Key to fetch from JObject", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JTokenParam(), "JToken", "JT", "Fetched JToken", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            JObjectGoo goo = null;
            string key = null;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref key);

            JObject jobj = goo.Value;
            JToken token = null;

            try
            {
                token = jobj.GetValue(key);
            }
            catch
            {

            }
            
            if (token != null) DA.SetData(0, new JTokenGoo(token));
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
            get { return new Guid("b70e62d2-141b-48f9-b023-b0579178c22c"); }
        }
    }
}