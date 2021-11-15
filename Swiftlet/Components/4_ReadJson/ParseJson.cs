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
    public class ParseJsonString : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ParseJSON class.
        /// </summary>
        public ParseJsonString()
          : base("Parse JSON String", "PJS",
              "Parse a string into a searchable JSON Object",
              NamingUtility.CATEGORY, NamingUtility.READ_JSON)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "JSON-formatted string to be parsed", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JTokenParam(), "JToken", "JT", "Parsed JSON Token", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string json = string.Empty;
            DA.GetData(0, ref json);

            JToken token = JToken.Parse(json);
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
                return Properties.Resources.Icons_parse_json_string_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("18b4fd6e-e6b9-4958-b8c5-1e0cf5a3d016"); }
        }
    }
}