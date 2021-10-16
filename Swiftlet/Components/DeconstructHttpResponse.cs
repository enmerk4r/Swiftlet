using System;
using System.Collections.Generic;

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
          : base("Deconstruct Response", "Deconstruct Response",
              "Deconstruct Http Response",
              NamingUtility.CATEGORY, NamingUtility.REQUESTS)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new HttpWebResponseParam(), "Response", "Response", "Http Web response", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("CharSet", "CS", "Character Set", GH_ParamAccess.item);
            pManager.AddTextParameter("Encoding", "E", "Content Encoding", GH_ParamAccess.item);
            pManager.AddTextParameter("Length", "L", "Content Length", GH_ParamAccess.item);
            pManager.AddTextParameter("Type", "T", "Content Type", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsCache", "IC", "Is From Cache", GH_ParamAccess.item);
            pManager.AddTimeParameter("Modified", "LM", "Last Modified", GH_ParamAccess.item);
            pManager.AddTextParameter("Method", "M", "Method", GH_ParamAccess.item);
            pManager.AddTextParameter("Uri", "R", "Response Uri", GH_ParamAccess.item);
            pManager.AddTextParameter("Server", "S", "Response Server", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Status Code", "SC", "Http Status Code", GH_ParamAccess.item);
            pManager.AddTextParameter("Status Description", "SD", "Http Status Description", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Supports Headers", "SH", "Supports Http Headers", GH_ParamAccess.item);
            pManager.AddTextParameter("Content", "C", "Response body content", GH_ParamAccess.item);
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
            DA.SetData(0, dto.CharacterSet);
            DA.SetData(1, dto.ContentEncoding);
            DA.SetData(2, dto.ContentLength.ToString());
            DA.SetData(3, dto.ContentType);
            DA.SetData(4, dto.IsFromCache);
            DA.SetData(5, dto.LastModified);
            DA.SetData(6, dto.Method);
            DA.SetData(7, dto.ResponseUri);
            DA.SetData(8, dto.ResponseServer);
            DA.SetData(9, dto.StatusCode);
            DA.SetData(10, dto.StatusDescription);
            DA.SetData(11, dto.SupportsHeaders);
            DA.SetData(12, dto.Content);
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
            get { return new Guid("6b60cc1e-e463-4997-b1b3-7d1835ec0c0b"); }
        }
    }
}