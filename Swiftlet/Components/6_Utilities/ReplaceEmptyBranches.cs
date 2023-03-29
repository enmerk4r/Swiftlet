using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    public class ReplaceEmptyBranches : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextToByteArray class.
        /// </summary>
        public ReplaceEmptyBranches()
          : base("Replace Empty Branches", "REB",
              "Substitutes all empty branches in a tree with a provided list of values.\nUseful for padding missing values in a web scraping scenario",
              NamingUtility.CATEGORY, NamingUtility.UTILITIES)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.senary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Tree", "T", "Input tree", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Replacement", "R", "List of values to replace empty branches with", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tree", "T", "Output tree with padded empty branches", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_Goo> tree = new GH_Structure<IGH_Goo>();
            List<IGH_Goo> padding = new List<IGH_Goo>();

            
            DA.GetDataTree(0, out tree);
            DA.GetDataList(1, padding);

            GH_Structure<IGH_Goo> newTree = tree.Duplicate();

            foreach (GH_Path path in tree.Paths)
            {
                var branch = tree.get_Branch(path);

                if (branch.Count == 0)
                {
                    for (int i = 0; i < padding.Count; i++)
                    {
                        newTree.Insert((padding[i]).Duplicate(), path, i);
                    }
                }
            }


            DA.SetDataTree(0, newTree);
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
                return Properties.Resources.Icons_replace_empty_branches_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1b49e53c-43ae-4a66-8615-c7db063465da"); }
        }
    }
}