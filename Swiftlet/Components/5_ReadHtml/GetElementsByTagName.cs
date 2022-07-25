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
    public class GetElementsByTagName : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetElementsByTagName class.
        /// </summary>
        public GetElementsByTagName()
          : base("Get Elements By Tag Name", "BYTAG",
              "Get HTML Elements by Tag Name",
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
            pManager.AddTextParameter("Tag", "T", "Name of the HTML tag", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Recursive", "R", "Determines whether to search for specified tags in all of the descendants, or only one level down", GH_ParamAccess.item, true);

            pManager[2].Optional = true;
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
            string tag = string.Empty;
            bool recursive = true;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref tag);
            DA.GetData(2, ref recursive);

            if (goo == null) return;
            if (string.IsNullOrEmpty(tag)) return;
            HtmlNode node = goo.Value;

            if (node == null) return;

            List<HtmlNode> nodes = new List<HtmlNode>();

            if (recursive)
            {
                nodes = node.Descendants(tag).ToList();
            }
            else
            {
                nodes = node.ChildNodes.Where(o => o.Name == tag).ToList();
            }
            DA.SetDataList(0, nodes.Select(o => new HtmlNodeGoo(o)));
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
            get { return new Guid("42EA89C9-46BD-4C81-8553-E0FD11CB652A"); }
        }
    }
}