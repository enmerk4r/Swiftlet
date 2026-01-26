using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateMultipartFormBody : GH_Component
    {
        public CreateMultipartFormBody()
          : base("Create Multipart Form Body", "CMFB",
              "Create a Request Body that supports multipart/form-data",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MultipartFieldParam(), "Fields", "F", "Multipart form fields", GH_ParamAccess.list);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<MultipartFieldGoo> fields = new List<MultipartFieldGoo>();
            DA.GetDataList(0, fields);

            List<MultipartField> fieldValues = fields.Select(f => f.Value).Where(f => f != null).ToList();
            RequestBodyMultipartForm form = new RequestBodyMultipartForm(fieldValues);
            DA.SetData(0, new RequestBodyGoo(form));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.Icons_create_multipart_form_body; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("15c68c1c-9e2c-4df3-83f3-8d9fb1fcd5cf"); }
        }
    }
}
