using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateJsonValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateJsonValue class.
        /// </summary>
        public CreateJsonValue()
          : base("Create JSON Value", "CJV",
              "Turn a Grasshopper value into a JSON value",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Value", "V", "A string, integer, number, DateTime or boolean. Leave empty for null.", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JValueParam(), "JValue", "JV", "JSON Value", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object obj = null;
            DA.GetData(0, ref obj);

            // Output null JValue when no input is provided
            if (obj == null)
            {
                DA.SetData(0, new JValueGoo(JValue.CreateNull()));
                return;
            }

            if (obj is GH_String)
            {
                GH_String str = obj as GH_String;
                DA.SetData(0, new JValueGoo(new JValue(str.Value)));
            }
            else if (obj is GH_Number)
            {
                GH_Number num = obj as GH_Number;
                DA.SetData(0, new JValueGoo(new JValue(num.Value)));
            }
            else if (obj is GH_Integer)
            {
                GH_Integer ghInt = obj as GH_Integer;
                DA.SetData(0, new JValueGoo(new JValue(ghInt.Value)));
            }
            else if (obj is GH_Boolean)
            {
                GH_Boolean ghBool = obj as GH_Boolean;
                DA.SetData(0, new JValueGoo(new JValue(ghBool.Value)));
            }
            else if (obj is GH_Time)
            {
                GH_Time time = obj as GH_Time;
                DA.SetData(0, new JValueGoo(new JValue(time.Value)));
            }
            else
            {
                throw new Exception($"Unable to create a JValue from object of type {obj.GetType()}");
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
                return Properties.Resources.Icons_create_json_value_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ca82e229-fcdb-415f-bd0b-5f37c1ef8c3f"); }
        }
    }
}