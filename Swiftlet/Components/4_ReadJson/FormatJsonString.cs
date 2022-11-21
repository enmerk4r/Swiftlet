using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class FormatJsonString : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FormatJsonString class.
        /// </summary>
        public FormatJsonString()
          : base("Format Json String", "FJS",
              "Prettify a JSON string by formatting it with proper indentations",
              NamingUtility.CATEGORY, NamingUtility.READ_JSON)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("JSON String", "J", "Unformatted JSON string", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Pretty JSON", "P", "Formatted JSON string (this helps with readability)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string json = string.Empty;
            DA.GetData(0, ref json);

            string value = string.Empty;

            try
            {
                JToken token = JToken.Parse(json);
                value = token.ToString();
            }
            catch (Exception ex)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                return;
            }
            
            DA.SetData(0, value);
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
                return Properties.Resources.Icons_format_json_string_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("37c912a2-2ab5-4926-8911-cb220b6adb25"); }
        }
    }
}