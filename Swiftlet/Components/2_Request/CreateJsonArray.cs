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
    public class CreateJsonArray : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateJsonArray class.
        /// </summary>
        public CreateJsonArray()
          : base("Create JSON Array", "CJA",
              "Combine a list of JTokens into a JArray",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JTokenParam(), "JTokens", "JT", "JTokens to be combined into an array", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JArrayParam(), "JArray", "JA", "Resulting JArray", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<JTokenGoo> goo = new List<JTokenGoo>();
            DA.GetDataList(0, goo);

            List<JToken> tokens = goo.Select(o => o.Value).ToList();
            JArray array = new JArray();
            tokens.ForEach(t => array.Add(t));

            DA.SetData(0, new JArrayGoo(array));
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
                return Properties.Resources.Icons_create_json_array_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("be0d2120-8576-44b1-934f-1a70568907a2"); }
        }
    }
}