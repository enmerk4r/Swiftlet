using System;
using System.Text;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class DeconstructBodyComponent : GH_Component
    {
        public DeconstructBodyComponent()
          : base("Deconstruct Body", "DB",
              "Deconstruct a Request Body into its components",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request body to deconstruct", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Content Type", "T", "The MIME content type", GH_ParamAccess.item);
            pManager.AddTextParameter("Text", "Tx", "Body content as text (UTF-8 decoded)", GH_ParamAccess.item);
            pManager.AddParameter(new ByteArrayParam(), "Bytes", "By", "Body content as byte array", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            RequestBodyGoo goo = null;
            if (!DA.GetData(0, ref goo))
            {
                return;
            }

            if (goo?.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid body provided");
                return;
            }

            var body = goo.Value;

            // Content type
            DA.SetData(0, body.ContentType ?? string.Empty);

            // Get bytes
            byte[] bytes = body.ToByteArray() ?? new byte[0];

            // Convert to text using UTF-8
            string text = string.Empty;
            try
            {
                text = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // If decoding fails, leave as empty string
            }

            DA.SetData(1, text);
            DA.SetData(2, new ByteArrayGoo(bytes));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_deconstruct_body;

        public override Guid ComponentGuid => new Guid("D5E6F7A8-B9C0-1234-5678-90ABCDEF1234");
    }
}
