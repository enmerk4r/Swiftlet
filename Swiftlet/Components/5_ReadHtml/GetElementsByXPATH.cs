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
    public class GetElementsByXPATH : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetElementsByXPATH class.
        /// </summary>
        public GetElementsByXPATH()
          : base("Get Elements By XPATH", "BYXPATH",
              "Get HTML elements via an XPATH expression",
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
            pManager.AddTextParameter("XPATH", "X", "XPATH Expression", GH_ParamAccess.item);
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
            string xpath = string.Empty;
            DA.GetData(0, ref goo);
            DA.GetData(1, ref xpath);

            if (goo == null) return;
            if (string.IsNullOrEmpty(xpath)) return;
            HtmlNode node = goo.Value;

            if (node == null) return;

            HtmlNodeCollection children = node.SelectNodes(xpath);
            List<HtmlNode> found = new List<HtmlNode>();
            if (children != null)
            {
                found.AddRange(children);
            }
            DA.SetDataList(0, found.Select(o => new HtmlNodeGoo(o)));
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
            get { return new Guid("DCED6D9C-0654-484A-AEC3-02D941F22EA9"); }
        }
    }
}