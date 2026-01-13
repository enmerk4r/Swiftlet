using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Util;
using System;
using System.Drawing;

namespace Swiftlet.Params
{
    public class McpToolCallRequestParam : GH_Param<McpToolCallRequestGoo>
    {
        public McpToolCallRequestParam()
            : base("MCP Tool Call Request", "R",
                 "Request for a pending MCP tool call",
                 NamingUtility.CATEGORY, NamingUtility.MCP, GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("E1F2A3B4-C5D6-7890-4567-901234567890");

        protected override Bitmap Icon => null;
    }
}
