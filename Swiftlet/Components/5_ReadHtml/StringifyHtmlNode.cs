using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using HtmlAgilityPack;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class StringifyHtmlNode : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the StringifyHtmlNode class.
        /// </summary>
        public StringifyHtmlNode()
          : base("Stringify HTML Node", "SHTML",
              "Convert an HTML Node to cleartext",
              NamingUtility.CATEGORY, NamingUtility.READ_HTML)
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.quinary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new HtmlNodeParam(), "HTML Node", "N", "HTML Node object", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("HTML", "H", "HTML Markup as text", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            HtmlNodeGoo goo = null;
            DA.GetData(0, ref goo);

            HtmlNode node = goo.Value;
            DA.SetData(0, node.OuterHtml);
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
            get { return new Guid("48819932-F530-4764-B9FA-FB44998CF11F"); }
        }
    }
}