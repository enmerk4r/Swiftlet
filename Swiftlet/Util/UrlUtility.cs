using Swiftlet.DataModels.Implementations;
using Swiftlet.DataModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.Util
{
    public static class UrlUtility
    {
        public static string AddQueryParams(string url, List<QueryParam> parameters)
        {
            if (parameters == null) return url;
            if (parameters.Count == 0) return url;
            else
            { 
                string fullUrl = string.Empty;
                fullUrl = $"{url}?";

                foreach (QueryParam param in parameters) fullUrl += param.ToQueryString() + "&";
                fullUrl = fullUrl.Substring(0, fullUrl.Length - 1);
                return fullUrl;
            }
            
        }
    }
}
