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
    public class ReadHtml : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ReadHtmlDocumentComponent class.
        /// </summary>
        public ReadHtml()
          : base("Read HTML", "HTML",
              "Read HTML Markup",
              NamingUtility.CATEGORY, NamingUtility.READ_HTML)

        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("HTML", "H", "HTML Markup", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new HtmlNodeParam(), "Node", "N", "Html Node", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string html = string.Empty;
            DA.GetData(0, ref html);

            HtmlDocument dom = new HtmlDocument();
            dom.LoadHtml(html);

            DA.SetData(0, new HtmlNodeGoo(dom.DocumentNode));
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
                return Properties.Resources.Icons_read_html_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("349B64D0-63E5-4263-85E1-788D03A71920"); }
        }
    }
}