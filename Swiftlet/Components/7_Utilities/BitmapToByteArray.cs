using System;
using System.IO;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class BitmapToByteArray : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public BitmapToByteArray()
          : base("Bitmap to Byte Array", "BTBA",
              "Converts a bitmap to a byte array",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.septenary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new BitmapParam(), "Bitmap", "B", "Input Bitmap", GH_ParamAccess.item);
            pManager.AddTextParameter("Format", "F", "Image format (BMP, JPEG, PNG, GIF, TIFF, EMF, WMF, EXIF, ICON)", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Output Byte Array", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            BitmapGoo goo = null;
            string format = string.Empty;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref format);

            byte[] array = new byte[0];
            System.Drawing.Imaging.ImageFormat imgFormat;

            switch (format.ToLower())
            {
                case "png": imgFormat = System.Drawing.Imaging.ImageFormat.Png; break;
                case "bmp": imgFormat = System.Drawing.Imaging.ImageFormat.Bmp; break;
                case "emf": imgFormat = System.Drawing.Imaging.ImageFormat.Emf; break;
                case "wmf": imgFormat = System.Drawing.Imaging.ImageFormat.Wmf; break;
                case "jpeg": imgFormat = System.Drawing.Imaging.ImageFormat.Jpeg; break;
                case "gif": imgFormat = System.Drawing.Imaging.ImageFormat.Gif; break;
                case "tiff": imgFormat = System.Drawing.Imaging.ImageFormat.Tiff; break;
                case "exif": imgFormat = System.Drawing.Imaging.ImageFormat.Exif; break;
                case "icon": imgFormat = System.Drawing.Imaging.ImageFormat.Icon; break;
                default: throw new Exception($"Format {format} is not supported");
            }

            using (var stream = new MemoryStream())
            {
                goo.Value.Save(stream, imgFormat);
                array = stream.ToArray();
            }

            DA.SetData(0, new ByteArrayGoo(array));
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
                return Properties.Resources.Icons_bitmap_to_byte_array_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3511b9b7-db9f-409c-8cf8-2a67e5bc952d"); } 
        }
    }
}