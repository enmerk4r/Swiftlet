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
    public class GetElementsByAttributeValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetElementsByClassName class.
        /// </summary>
        public GetElementsByAttributeValue()
          : base("Get Elements By Attribute Value", "BYATTR",
              "Get all HTML elements where a certain attribute is equal to a certain value",
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
            pManager.AddTextParameter("Attribute", "A", "Name of the HTML attribute", GH_ParamAccess.item);
            pManager.AddTextParameter("Value", "V", "Attribute value to search for", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Recursive", "R", "Determines whether to search for specified attribute in all of the descendants, or only one level down", GH_ParamAccess.item, true);

            pManager[3].Optional = true;
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
            string attrName = string.Empty;
            string attrValue = string.Empty;
            bool recursive = true;
            DA.GetData(0, ref goo);
            DA.GetData(1, ref attrName);
            DA.GetData(2, ref attrValue);
            DA.GetData(3, ref recursive);

            if (goo == null) return;
            if (string.IsNullOrEmpty(attrName)) return;
            
            HtmlNode node = goo.Value;

            if (node == null) return;

            List<HtmlNodeGoo> matching = new List<HtmlNodeGoo>();
            if (recursive)
            {
                HtmlNodeCollection children = node.SelectNodes($".//*[@{attrName}='{attrValue}']");
                if (children != null)
                {
                    foreach (HtmlNode child in children)
                    {
                        matching.Add(new HtmlNodeGoo(child));
                    }
                }
            }
            else
            {
                var children = node.ChildNodes;
                if (children != null)
                {
                    foreach (HtmlNode child in children)
                    {
                        if (child != null)
                        {
                            if (child.Attributes.Contains(attrName))
                            {
                                if (child.Attributes[attrName].Value == attrValue)
                                {
                                    matching.Add(new HtmlNodeGoo(child));
                                }
                            }
                        }
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
                return Properties.Resources.Icons_get_elements_by_attribute_value_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("560a0b7b-8c6a-4299-9433-6022657d178c"); }
        }
    }
}