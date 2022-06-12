﻿using Newtonsoft.Json.Linq;
using Swiftlet.DataModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Implementations
{
    public class HttpHeader : IHttpHeader
    {
        public string Key { get; private set; }
        public string Value { get; private set; }

        public void SetValue(string value)
        {
            this.Value = value;
        }

        public HttpHeader(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        public string ToJsonString()
        {
            JObject obj = new JObject();
            obj.Add(this.Key, this.Value);
            return obj.ToString().Replace("\n", "").Replace("\r", "");
        }
    }
}
