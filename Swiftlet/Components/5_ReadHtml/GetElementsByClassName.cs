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
    public class GetElementsByClassName : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetElementsByClassName class.
        /// </summary>
        public GetElementsByClassName()
          : base("Get Elements By Class Name", "BYCLASS",
              "Get all HTML elements by a specific class name",
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
            pManager.AddTextParameter("Class", "C", "Name of the HTML class", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Recursive", "R", "Determines whether to search for specified attribute in all of the descendants, or only one level down", GH_ParamAccess.item, true);

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
            string className = string.Empty;
            bool recursive = true;

            DA.GetData(0, ref goo);
            DA.GetData(1, ref className);
            DA.GetData(2, ref recursive);

            if (goo == null) return;
            if (string.IsNullOrEmpty(className)) return;
            HtmlNode node = goo.Value;

            if (node == null) return;

            List<HtmlNode> children = new List<HtmlNode>();
            List<HtmlNodeGoo> matching = new List<HtmlNodeGoo>();

            if (recursive)
            {
                children = node.Descendants().ToList();

            }
            else
            {
                var directChildren = node.ChildNodes;
                if (directChildren != null)
                {
                    children = directChildren.ToList();
                }
            }

            foreach (HtmlNode child in children)
            {
                if (child.Attributes.Contains("class"))
                {
                    string classStr = child.Attributes["class"].Value;
                    List<string> parts = classStr.Split(' ').ToList();
                    if (parts.Contains(className))
                    {
                        matching.Add(new HtmlNodeGoo(child));
                    }
                }
            }

            DA.SetDataList(0, matching);
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
                return Properties.Resources.Icons_get_elements_by_class_name_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7D89F228-1FDD-4B38-81DA-ECE9C8634A74"); }
        }
    }
}