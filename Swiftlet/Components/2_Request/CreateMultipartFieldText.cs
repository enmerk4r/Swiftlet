using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateMultipartFieldText : GH_Component
    {
        public CreateMultipartFieldText()
          : base("Create Multipart Field Text", "CMFT",
              "Create a text multipart/form-data field",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Field name (optional)", GH_ParamAccess.item);
            pManager.AddTextParameter("Text", "T", "Field text value", GH_ParamAccess.item);
            pManager.AddTextParameter("ContentType", "C", "Field content type", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new MultipartFieldParam(), "Field", "F", "Multipart field", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = string.Empty;
            string text = string.Empty;
            string contentType = string.Empty;

            DA.GetData(0, ref name);
            DA.GetData(1, ref text);
            DA.GetData(2, ref contentType);

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = ContentTypeUtility.TextPlain;
            }

            MultipartField field = new MultipartField(name ?? string.Empty, text ?? string.Empty, contentType);
            DA.SetData(0, new MultipartFieldGoo(field));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.Icons_create_multipart_feild_text; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("7df2ccdb-0d64-4ca4-b1b9-506b46b52338"); }
        }
    }
}
