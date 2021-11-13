using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components._4_ReadJson
{
    public class SeparateJsonTokens : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SeparateJsonTokens class.
        /// </summary>
        public SeparateJsonTokens()
          : base("Separate JSON Tokens", "SJT",
              "Separate tokens into JObjects, JArrays and JValues",
              NamingUtility.CATEGORY, NamingUtility.READ_JSON)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JTokenParam(), "JTokens", "JT", "JTokens to be separated", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JObjectParam(), "JObjects", "JO", "Separated JObjects", GH_ParamAccess.list);
            pManager.AddParameter(new JArrayParam(), "JArrays", "JA", "Separated JArrays", GH_ParamAccess.list);
            pManager.AddParameter(new JValueParam(), "JValues", "JV", "Separated JValues", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<JTokenGoo> tokenGoo = new List<JTokenGoo>();
            DA.GetDataList(0, tokenGoo);

            List<JObjectGoo> objectGoo = new List<JObjectGoo>();
            List<JArrayGoo> arrayGoo = new List<JArrayGoo>();
            List<JValueGoo> valueGoo = new List<JValueGoo>();

            foreach(JTokenGoo goo in tokenGoo)
            {
                JObjectGoo objGoo = null;
                JArrayGoo arrGoo = null;
                JValueGoo valGoo = null;

                if (goo.CastTo<JObjectGoo>(ref objGoo))
                {
                    objectGoo.Add(objGoo);
                }
                else if (goo.CastTo<JArrayGoo>(ref arrGoo))
                {
                    arrayGoo.Add(arrGoo);
                }
                else if (goo.CastTo<JValueGoo>(ref valGoo))
                {
                    valueGoo.Add(valGoo);
                }
            }

            DA.SetDataList(0, objectGoo);
            DA.SetDataList(1, arrayGoo);
            DA.SetDataList(2, valueGoo);
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
            get { return new Guid("034d6e3b-ae02-4d7b-995b-88f752f36790"); }
        }
    }
}