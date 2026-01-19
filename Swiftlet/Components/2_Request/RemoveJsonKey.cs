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
    public class RemoveJsonKey : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RemoveJsonToken class.
        /// </summary>
        public RemoveJsonKey()
          : base("Remove JSON Key", "RJK",
              "Remove a key from JObject",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.septenary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JObjectParam(), "JObject", "JO", "JObject to remove the key from", GH_ParamAccess.item);
            pManager.AddTextParameter("Key", "K", "Key to be removed", GH_ParamAccess.item);
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
            JObjectGoo goo = null;
            string key = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref key);

            JObject obj = goo.Value.DeepClone() as JObject;
            try
            {
                obj.Remove(key);
            }
            catch
            {
                // pass
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
                return Properties.Resources.Icons_remove_json_key_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("452cf748-8d55-42f8-b141-499b8f931891"); }
        }
    }
}