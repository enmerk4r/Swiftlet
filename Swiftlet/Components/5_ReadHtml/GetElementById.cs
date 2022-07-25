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
    public class GetElementById : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetElementById class.
        /// </summary>
        public GetElementById()
          : base("Get Element By Id", "BYID",
              "Get an HTML element by ID",
              NamingUtility.CATEGORY, NamingUtility.READ_HTML)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new HtmlNodeParam(), "Parent", "P", "Parent node", GH_ParamAccess.item);
            pManager.AddTextParameter("ID", "I", "HTML Element ID", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new HtmlNodeParam(), "Element", "E", "Found element", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            HtmlNodeGoo goo = null;
            string elementId = string.Empty;
            DA.GetData(0, ref goo);
            DA.GetData(1, ref elementId);

            if (goo == null) return;
            if (string.IsNullOrEmpty(elementId)) return;
            HtmlNode node = goo.Value;

            if (node == null) return;

            HtmlDocument tempDoc = new HtmlDocument();
            tempDoc.LoadHtml(node.OuterHtml);

            HtmlNode element = tempDoc.GetElementbyId(elementId);
            if (element == null) return;
            DA.SetData(0, new HtmlNodeGoo(element));
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
            get { return new Guid("799427C4-0E1B-4ADB-B3A0-1CE33DDF7A41"); }
        }
    }
}