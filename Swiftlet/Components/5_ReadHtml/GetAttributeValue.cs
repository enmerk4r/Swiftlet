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
    public class GetAttributeValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetAttributeValue class.
        /// </summary>
        public GetAttributeValue()
          : base("Get Attribute Value", "GATTR",
              "Get the value of an HTML attribute",
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
            pManager.AddTextParameter("Attribute", "A", "Name of the HTML attribute", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Value", "V", "HTML Attribute value", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            HtmlNodeGoo goo = null;
            string attr = string.Empty;
            DA.GetData(0, ref goo);
            DA.GetData(1, ref attr);

            if (goo == null) return;
            if (string.IsNullOrEmpty(attr)) return;
            HtmlNode node = goo.Value;

            if (node == null) return;
            if (!node.Attributes.Contains(attr)) return;

            DA.SetData(0, node.Attributes[attr].Value);
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
            get { return new Guid("82D23920-743C-41B8-B5FF-085CB3F6F086"); }
        }
    }
}