using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;
using System;

namespace Swiftlet.Components
{
    public class McpToolResponseComponent : GH_Component
    {
        public McpToolResponseComponent()
            : base("MCP Tool Response", "Respond",
                "Sends a response back to the MCP client for a tool call.",
                NamingUtility.CATEGORY, NamingUtility.MCP)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new McpToolCallRequestParam(), "Request", "R", "The tool call request from MCP Server", GH_ParamAccess.item);
            pManager.AddParameter(new JTokenParam(), "Content", "C", "Response content (JToken)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Is Error", "E", "Whether this is an error response", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Success", "OK", "True if the response was sent successfully", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            McpToolCallRequestGoo requestGoo = null;
            JTokenGoo contentGoo = null;
            bool isError = false;

            if (!DA.GetData(0, ref requestGoo))
            {
                DA.SetData(0, false);
                return;
            }

            DA.GetData(1, ref contentGoo);
            DA.GetData(2, ref isError);

            if (requestGoo?.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid request provided");
                DA.SetData(0, false);
                return;
            }

            var request = requestGoo.Value;

            if (request.HasResponded)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Response has already been sent for this call");
                DA.SetData(0, false);
                return;
            }

            JToken content = contentGoo?.Value ?? JValue.CreateString(string.Empty);

            bool success;
            if (isError)
            {
                success = request.SendError(-32000, content.ToString());
            }
            else
            {
                success = request.SendJsonResponse(content);
            }

            if (!success)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to send response");
            }

            DA.SetData(0, success);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("B8C9D0E1-F2A3-4567-1234-678901234567");
    }
}
