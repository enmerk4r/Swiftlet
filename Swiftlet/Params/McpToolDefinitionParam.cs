using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Drawing;

namespace Swiftlet.Params
{
    public class McpToolDefinitionParam : GH_Param<McpToolDefinitionGoo>
    {
        public McpToolDefinitionParam()
            : base("MCP Tool Definition", "T",
                 "A tool definition for an MCP server",
                 NamingUtility.CATEGORY, NamingUtility.MCP, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        public override Guid ComponentGuid => new Guid("D0E1F2A3-B4C5-6789-3456-890123456789");

        protected override Bitmap Icon => Properties.Resources.Icons_mcp_tool_definition;
    }
}
