using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using HtmlAgilityPack;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components._5_ReadHtml
{
    public class GetHtmlAttributes : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetHtmlAttributes class.
        /// </summary>
        public GetHtmlAttributes()
          : base("Get Html Attributes", "GATTRS",
              "Get all attributes and values of an HTML element",
              NamingUtility.CATEGORY, NamingUtility.READ_HTML)
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new HtmlNodeParam(), "Node", "N", "HTML node", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Names", "N", "List of HTML Attribute names", GH_ParamAccess.list);
            pManager.AddTextParameter("Values", "V", "List of HTML Attribute values", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            HtmlNodeGoo goo = null;
            DA.GetData(0, ref goo);

            if (goo == null) return;
            HtmlNode node = goo.Value;

            if (node == null) return;
            List<HtmlAttribute> attrs = node.Attributes.ToList();
            List<string> keys = attrs.Select(o => o.Name).ToList();
            List<string> values = attrs.Select(o => o.Value).ToList();

            DA.SetDataList(0, keys);
            DA.SetDataList(1, values);
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
            get { return new Guid("08D3C43F-1537-4C91-9CC0-D429FF62A89C"); }
        }
    }
}