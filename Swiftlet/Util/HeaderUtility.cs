using Swiftlet.DataModels.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Util
{
    public static class HeaderUtility
    {
        public static string ContentType => "Content-Type";

        private static Dictionary<ContentType, string> _headerDict = new Dictionary<ContentType, string>()
        {
            { DataModels.Enums.ContentType.Text, "text/plain"},
            { DataModels.Enums.ContentType.JavaScript, "text/javascript" },
            { DataModels.Enums.ContentType.JSON, "application/json" },
            { DataModels.Enums.ContentType.HTML, "text/html" },
            { DataModels.Enums.ContentType.XML, "application/xml" }
        };

        public static string GetContentType(ContentType type)
        {
            return _headerDict[type];
        }
    }
}
