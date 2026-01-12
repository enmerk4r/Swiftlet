using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Util
{
    public static class ContentTypeUtility
    {
        public static string Header => "Content-Type";

        public const string TextPlain = "text/plain";
        public const string JavaScript = "text/javascript";
        public const string ApplicationJson = "application/json";
        public const string TextHtml = "text/html";
        public const string ApplicationXml = "application/xml";
        public const string ApplicationOctetStream = "application/octet-stream";
        public const string MultipartForm = "multipart/form-data";

        public static string ContentTypeToMessage(string contentType)
        {
            switch (contentType.ToLower())
            {
                case TextPlain:
                    return "Text";
                case JavaScript:
                    return "JavaScript";
                case ApplicationJson:
                    return "JSON";
                case ApplicationXml:
                    return "XML";
                case TextHtml:
                    return "HTML";
                default:
                    return "Custom";
            }
        }
    }
}
