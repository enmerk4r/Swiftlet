using System;
using System.Drawing;
using Grasshopper.Kernel;
using QRCoder;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GenerateQRCode : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public GenerateQRCode()
          : base("Generate QR code", "QR",
              "Generates a QR code from a string",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.septenary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Text to encode as a QR code (e.g. a URL)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Pixels", "P", "Pixels per module", GH_ParamAccess.item, 20);
            pManager.AddColourParameter("Dark", "D", "Dark color", GH_ParamAccess.item, Color.Black);
            pManager.AddColourParameter("Light", "L", "Light color", GH_ParamAccess.item, Color.White);
            pManager.AddBooleanParameter("Quiet", "Q", "Draw quiet zones", GH_ParamAccess.item, true);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Output bitmap", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string text = string.Empty;
            int numPixels = 20;
            Color dark = Color.Black;
            Color light = Color.White;
            bool quiet = true;

            if (DA.GetData(0, ref text))
            {
                DA.GetData(1, ref numPixels);
                DA.GetData(2, ref dark);
                DA.GetData(3, ref light);
                DA.GetData(4, ref quiet);

                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap img = qrCode.GetGraphic(numPixels, dark, light, quiet);

                DA.SetData(0, new BitmapGoo(img));
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
                return Properties.Resources.Icons_generate_qr_code_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("fe44460c-871a-4098-a948-48aa02977410"); } 
        }
    }
}