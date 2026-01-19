using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    /// <summary>
    /// Creates a request body with application/x-www-form-urlencoded content type.
    /// Commonly used for HTML form submissions and OAuth token endpoints.
    /// </summary>
    public class CreateFormUrlEncodedBody : GH_Component
    {
        public CreateFormUrlEncodedBody()
          : base("Create Form URL Encoded Body", "CFUEB",
              "Create a request body with application/x-www-form-urlencoded content type. Commonly used for HTML form submissions and OAuth token endpoints.",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Keys", "K", "Form field names", GH_ParamAccess.list);
            pManager.AddTextParameter("Values", "V", "Form field values", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Form URL encoded request body", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> keys = new List<string>();
            List<string> values = new List<string>();

            DA.GetDataList(0, keys);
            DA.GetDataList(1, values);

            if (keys.Count != values.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Keys and values must have the same count. Got {keys.Count} keys and {values.Count} values.");
                return;
            }

            if (keys.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No form fields provided");
            }

            try
            {
                RequestBodyFormUrlEncoded body = new RequestBodyFormUrlEncoded(keys, values);
                DA.SetData(0, new RequestBodyGoo(body));

                this.Message = "x-www-form-urlencoded";
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_create_form_url_encoded_body;

        public override Guid ComponentGuid => new Guid("C9D0E1F2-A3B4-2233-7E8F-90011223344A");
    }
}
