using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Swiftlet.Components
{
    public class DefineToolComponent : GH_Component
    {
        public DefineToolComponent()
            : base("Define Tool", "Tool",
                "Defines an MCP tool that can be called by AI clients.",
                NamingUtility.CATEGORY, NamingUtility.MCP)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Tool name (e.g., 'compute_area')", GH_ParamAccess.item);
            pManager.AddTextParameter("Description", "D", "Description of what the tool does", GH_ParamAccess.item);
            pManager.AddParameter(new McpToolParameterParam(), "Parameters", "P", "Tool parameter definitions", GH_ParamAccess.list);

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new McpToolDefinitionParam(), "Tool", "T", "The tool definition", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = string.Empty;
            string description = string.Empty;
            List<McpToolParameterGoo> parameterGoos = new List<McpToolParameterGoo>();

            if (!DA.GetData(0, ref name)) return;
            if (!DA.GetData(1, ref description)) return;
            DA.GetDataList(2, parameterGoos);

            if (string.IsNullOrWhiteSpace(name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tool name cannot be empty");
                return;
            }

            // Extract parameters from Goo wrappers
            var parameters = parameterGoos
                .Where(g => g?.Value != null)
                .Select(g => g.Value)
                .ToList();

            var tool = new McpToolDefinition(name, description, parameters);
            DA.SetData(0, new McpToolDefinitionGoo(tool));
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("E5F6A7B8-C9D0-1234-EF01-345678901234");
    }
}
