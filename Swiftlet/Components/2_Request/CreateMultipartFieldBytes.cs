using System;
using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class CreateMultipartFieldBytes : GH_Component
    {
        public CreateMultipartFieldBytes()
          : base("Create Multipart Field Bytes", "CMFBy",
              "Create a byte multipart/form-data field",
              NamingUtility.CATEGORY, NamingUtility.REQUEST)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Field name (optional)", GH_ParamAccess.item);
            pManager.AddParameter(new ByteArrayParam(), "Bytes", "B", "Field bytes", GH_ParamAccess.item);
            pManager.AddTextParameter("FileName", "F", "Optional filename", GH_ParamAccess.item);
            pManager.AddTextParameter("ContentType", "C", "Field content type", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new MultipartFieldParam(), "Field", "F", "Multipart field", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = string.Empty;
            ByteArrayGoo bytesGoo = null;
            string fileName = string.Empty;
            string contentType = string.Empty;

            DA.GetData(0, ref name);
            DA.GetData(1, ref bytesGoo);
            DA.GetData(2, ref fileName);
            DA.GetData(3, ref contentType);

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = ContentTypeUtility.ApplicationOctetStream;
            }

            byte[] bytes = bytesGoo?.Value ?? new byte[0];
            MultipartField field = new MultipartField(name ?? string.Empty, bytes, fileName, contentType);
            DA.SetData(0, new MultipartFieldGoo(field));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.Icons_create_multipart_field_bytes; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("1a6521b4-40da-40ad-bc43-9b067a9ea6a8"); }
        }
    }
}
