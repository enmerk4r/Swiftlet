using Swiftlet.DataModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Implementations
{
    public class QueryParam : IQueryParam
    {
        public string Key { get; private set; }

        public string Value { get; private set; }

        public void SetValue(string value)
        {
            this.Value = value;
        }

        public string ToQueryString()
        {
            return $"{this.Key}={this.Value}";
        }

        public QueryParam(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}
