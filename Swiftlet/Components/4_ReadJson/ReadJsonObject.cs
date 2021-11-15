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
    public class ReadJsonObject : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetJsonObjectKeys class.
        /// </summary>
        public ReadJsonObject()
          : base("Read JSON Object", "RJO",
              "Get all keys and values from JObject",
              NamingUtility.CATEGORY, NamingUtility.READ_JSON)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JTokenParam(), "JObject", "JO", "JObject to get the keys and values from", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Keys", "K", "JObject keys", GH_ParamAccess.list);
            pManager.AddParameter(new JTokenParam(), "JTokens", "JT", "Parsed JSON Tokens", GH_ParamAccess.list);
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
            List<JTokenGoo> tokens = new List<JTokenGoo>();

            JToken token = goo.Value;

            if (token is JObject)
            {
                JObject obj = token as JObject;
                keys = obj.Properties().Select(p => p.Name).ToList();
                foreach(string k in keys)
                {
                    JToken value = obj.GetValue(k);
                    tokens.Add(new JTokenGoo(value));
                }
                DA.SetDataList(0, keys);
                DA.SetDataList(1, tokens);
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
                return Properties.Resources.Icons_read_json_object_24x24;
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