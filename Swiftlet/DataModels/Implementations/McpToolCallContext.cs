using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;

namespace Swiftlet.DataModels.Implementations
{
    public class McpToolCallContext
    {
        public string SessionId { get; private set; }
        public object RequestId { get; private set; }
        public string ToolName { get; private set; }
        public JObject Arguments { get; private set; }
        public HttpListenerContext HttpContext { get; private set; }
        public bool HasResponded { get; set; }

        public McpToolCallContext(
            string sessionId,
            object requestId,
            string toolName,
            JObject arguments,
            HttpListenerContext httpContext)
        {
            this.SessionId = sessionId;
            this.RequestId = requestId;
            this.ToolName = toolName;
            this.Arguments = arguments;
            this.HttpContext = httpContext;
            this.HasResponded = false;
        }

        public bool SendResponse(string textContent)
        {
            if (HasResponded || HttpContext == null)
                return false;

            try
            {
                var result = new JObject
                {
                    ["content"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "text",
                            ["text"] = textContent
                        }
                    }
                };

                return SendJsonRpcResponse(result);
            }
            catch
            {
                return false;
            }
        }

        public bool SendJsonResponse(JToken jsonContent)
        {
            if (HasResponded || HttpContext == null)
                return false;

            try
            {
                var result = new JObject
                {
                    ["content"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "text",
                            ["text"] = jsonContent.ToString()
                        }
                    }
                };

                return SendJsonRpcResponse(result);
            }
            catch
            {
                return false;
            }
        }

        public bool SendError(int code, string message)
        {
            if (HasResponded || HttpContext == null)
                return false;

            try
            {
                var response = new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = RequestId != null ? JToken.FromObject(RequestId) : JValue.CreateNull(),
                    ["error"] = new JObject
                    {
                        ["code"] = code,
                        ["message"] = message
                    }
                };

                return WriteResponse(response);
            }
            catch
            {
                return false;
            }
        }

        private bool SendJsonRpcResponse(JToken result)
        {
            var response = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = RequestId != null ? JToken.FromObject(RequestId) : JValue.CreateNull(),
                ["result"] = result
            };

            return WriteResponse(response);
        }

        private bool WriteResponse(JObject response)
        {
            try
            {
                var httpResponse = HttpContext.Response;
                httpResponse.ContentType = "application/json";
                httpResponse.StatusCode = 200;

                byte[] buffer = Encoding.UTF8.GetBytes(response.ToString());
                httpResponse.ContentLength64 = buffer.Length;
                httpResponse.OutputStream.Write(buffer, 0, buffer.Length);
                httpResponse.OutputStream.Close();

                HasResponded = true;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
