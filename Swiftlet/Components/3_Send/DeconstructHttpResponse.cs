using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class DeconstructHttpResponse : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DeconstructHttpResponse class.
        /// </summary>
        public DeconstructHttpResponse()
          : base("Deconstruct Response", "DR",
              "Deconstruct Http Response",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new HttpWebResponseParam(), "Response", "R", "Http Web response", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Version", "V", "The HTTP message version. The default is 1.1", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Status", "S", "Http status code", GH_ParamAccess.item);
            pManager.AddTextParameter("Reason", "R", "The reason phrase which typically is sent by servers together with the status code", GH_ParamAccess.item);
            pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "The collection of HTTP response headers", GH_ParamAccess.list);
            pManager.AddBooleanParameter("IsSuccess", "iS", "Indicates if the HTTP response was successful", GH_ParamAccess.item);
            pManager.AddTextParameter("Content", "C", "Response content", GH_ParamAccess.item);
            pManager.AddParameter(new ByteArrayParam(), "Byte Array", "A", "Response data as byte array", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            HttpWebResponseGoo goo = null;
            DA.GetData(0, ref goo);

            HttpResponseDTO dto = goo.Value;
            DA.SetData(0, dto.Version);
            DA.SetData(1, dto.StatusCode);
            DA.SetData(2, dto.ReasonPhrase);
            DA.SetDataList(3, dto.Headers.Select(h => new HttpHeaderGoo(h)));
            DA.SetData(4, dto.IsSuccessStatusCode);
            DA.SetData(5, dto.Content);
            DA.SetData(6, new ByteArrayGoo(dto.Bytes));
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
                return Properties.Resources.Icons_deconstruct_response_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6b60cc1e-e463-4997-b1b3-7d1835ec0c0b"); }
        }
    }
}