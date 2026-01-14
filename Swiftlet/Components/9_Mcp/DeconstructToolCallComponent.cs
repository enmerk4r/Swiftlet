using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;

namespace Swiftlet.Components
{
    public class DeconstructToolCallComponent : GH_Component
    {
        public DeconstructToolCallComponent()
            : base("Deconstruct Tool Call", "DeCall",
                "Extracts data from an incoming MCP tool call.",
                NamingUtility.CATEGORY, NamingUtility.MCP)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new McpToolCallRequestParam(), "Request", "R", "The tool call request from MCP Server", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new McpToolCallRequestParam(), "Request", "R", "Pass-through request for MCP Tool Response", GH_ParamAccess.item);
            pManager.AddTextParameter("Tool", "T", "Tool name that was called", GH_ParamAccess.item);
            pManager.AddParameter(new JObjectParam(), "Arguments", "A", "The arguments as a JSON object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            McpToolCallRequestGoo requestGoo = null;

            if (!DA.GetData(0, ref requestGoo))
            {
                return;
            }

            if (requestGoo?.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid request provided");
                return;
            }

            var request = requestGoo.Value;

            // Pass through the request unchanged
            DA.SetData(0, requestGoo);

            // Tool name
            DA.SetData(1, request.ToolName);

            // Arguments as JObject
            if (request.Arguments != null)
            {
                DA.SetData(2, new JObjectGoo(request.Arguments));
            }
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("A7B8C9D0-E1F2-3456-0123-567890123456");
    }
}
