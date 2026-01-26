using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components._2_Request
{
    public class SetJsonKey : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the AddJsonToken class.
        /// </summary>
        public SetJsonKey()
          : base("Set JSON Key", "SJK",
              "Add or modify a JObject key",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }
#if RHINO8
        public override GH_Exposure Exposure => GH_Exposure.octonary;
#else
        public override GH_Exposure Exposure => GH_Exposure.septenary;
#endif

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JObjectParam(), "JObject", "JO", "JObject to add the JToken to", GH_ParamAccess.item);
            pManager.AddTextParameter("Key", "K", "JToken key", GH_ParamAccess.item);
            pManager.AddParameter(new JTokenParam(), "JToken", "JT", "JToken to be set on the JObject", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JObjectParam(), "JObject", "JO", "Updated JObject", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            JObjectGoo objGoo = null;
            string key = string.Empty;
            JTokenGoo tokenGoo = null;

            DA.GetData(0, ref objGoo);
            DA.GetData(1, ref key);
            DA.GetData(2, ref tokenGoo);

            JObject obj = objGoo.Value.DeepClone() as JObject;
            JToken token = tokenGoo.Value.DeepClone();

            try
            {
                obj.Add(key, token);
            }
            catch
            {
                obj[key] = token;
            }

            DA.SetData(0, new JObjectGoo(obj));

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
                return Properties.Resources.Icons_set_json_key_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("306ee7f8-1c8e-4500-a3b9-8215b8934b43"); }
        }
    }
}