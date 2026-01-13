using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Drawing;

namespace Swiftlet.Params
{
    public class McpToolParameterParam : GH_Param<McpToolParameterGoo>
    {
        public McpToolParameterParam()
            : base("MCP Tool Parameter", "P",
                 "A parameter definition for an MCP tool",
                 NamingUtility.CATEGORY, NamingUtility.MCP, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("C9D0E1F2-A3B4-5678-2345-789012345678");

        protected override Bitmap Icon => null;
    }
}
