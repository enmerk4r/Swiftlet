using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ReplaceEmptyBranchesComponent : GH_Component
{
    public ReplaceEmptyBranchesComponent()
        : base("Replace Empty Branches", "REB", "Substitutes all empty branches in a tree with a provided list of values.\nUseful for padding missing values in a web scraping scenario", ShellNaming.Category, ShellNaming.Utilities)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.senary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Tree", "T", "Input tree", GH_ParamAccess.tree);
        pManager.AddGenericParameter("Replacement", "R", "List of values to replace empty branches with", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Tree", "T", "Output tree with padded empty branches", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (!DA.GetDataTree(0, out GH_Structure<IGH_Goo>? tree))
        {
            return;
        }

        var replacement = new List<IGH_Goo>();
        DA.GetDataList(1, replacement);

        GH_Structure<IGH_Goo> result = tree.Duplicate();
        foreach (GH_Path path in tree.Paths)
        {
            var branch = tree.get_Branch(path);
            if (branch.Count > 0)
            {
                continue;
            }

            for (int index = 0; index < replacement.Count; index++)
            {
                result.Insert(replacement[index].Duplicate(), path, index);
            }
        }

        DA.SetDataTree(0, result);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("1B49E53C-43AE-4A66-8615-C7DB063465DA");
}

