using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using HtmlAgilityPack;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class GetHtmlNodeChildren : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetHtmlNodeChildren class.
        /// </summary>
        public GetHtmlNodeChildren()
          : base("Get Child Nodes", "GCH",
              "Get all child nodes of an HTML node ",
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
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new HtmlNodeParam(), "Children", "C", "A list of child nodes", GH_ParamAccess.list);
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
            HtmlNodeCollection children = node.ChildNodes;
            List<HtmlNode> found = new List<HtmlNode>();
            if (children != null)
            {
                found.AddRange(children);
            }
            List<HtmlNodeGoo> childrenGoo = found.Select(o => new HtmlNodeGoo(o)).ToList();
            
            DA.SetDataList(0, childrenGoo);
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
            get { return new Guid("2E2FBF29-4CCE-4EE2-9055-2A3D7F3A60FE"); }
        }
    }
}