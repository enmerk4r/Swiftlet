using Grasshopper.Kernel;
using Swiftlet.DataModels.Implementations;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;

namespace Swiftlet.Components
{
    public class DefineToolParameterComponent : GH_Component
    {
        public DefineToolParameterComponent()
            : base("Define Tool Parameter", "Param",
                "Defines a parameter for an MCP tool.",
                NamingUtility.CATEGORY, NamingUtility.MCP)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Parameter name", GH_ParamAccess.item);
            pManager.AddTextParameter("Type", "T", "JSON Schema type: string, number, integer, boolean, object, array", GH_ParamAccess.item, "string");
            pManager.AddTextParameter("Description", "D", "Parameter description", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("Required", "R", "Is this parameter required?", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new McpToolParameterParam(), "Parameter", "P", "The parameter definition", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = string.Empty;
            string type = "string";
            string description = string.Empty;
            bool required = true;

            if (!DA.GetData(0, ref name)) return;
            DA.GetData(1, ref type);
            DA.GetData(2, ref description);
            DA.GetData(3, ref required);

            if (string.IsNullOrWhiteSpace(name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Parameter name cannot be empty");
                return;
            }

            var parameter = new McpToolParameter(name, type, description, required);
            DA.SetData(0, new McpToolParameterGoo(parameter));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_define_tool_parameter;

        public override Guid ComponentGuid => new Guid("F6A7B8C9-D0E1-2345-F012-456789012345");
    }
}
