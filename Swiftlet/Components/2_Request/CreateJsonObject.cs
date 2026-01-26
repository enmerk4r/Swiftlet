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
    public class CreateJsonObject : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateJsonObject class.
        /// </summary>
        public CreateJsonObject()
          : base("Create JSON Object", "CJO",
              "Create a JObject from keys and values",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Keys", "K", "List of JObject keys", GH_ParamAccess.list);
            pManager.AddParameter(new JTokenParam(), "Values", "V", "List of JObject values", GH_ParamAccess.list);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JObjectParam(), "JObject", "O", "Resulting JObject", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> keys = new List<string>();
            List<JTokenGoo> values = new List<JTokenGoo>();

            DA.GetDataList(0, keys);
            DA.GetDataList(1, values);

            if (keys.Count != values.Count) throw new Exception("The number of keys must match the number of values");

            JObject obj = new JObject();

            for (int i = 0; i < keys.Count; i++)
            {
                string k = keys[i];
                JToken v = values[i].Value;

                obj.Add(k, v);
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
                return Properties.Resources.Icons_create_json_object_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2892726b-fb0e-424a-877d-353f8ac18dc5"); }
        }
    }
}