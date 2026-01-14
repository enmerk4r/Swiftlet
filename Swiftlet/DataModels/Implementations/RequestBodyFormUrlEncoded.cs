using Swiftlet.DataModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Swiftlet.DataModels.Implementations
{
    /// <summary>
    /// Request body with application/x-www-form-urlencoded content type.
    /// Used for traditional HTML form submissions and many OAuth token endpoints.
    /// </summary>
    public class RequestBodyFormUrlEncoded : IRequestBody
    {
        public string ContentType => "application/x-www-form-urlencoded";

        public object Value => _formData;

        private readonly List<KeyValuePair<string, string>> _formData;

        public RequestBodyFormUrlEncoded()
        {
            _formData = new List<KeyValuePair<string, string>>();
        }

        public RequestBodyFormUrlEncoded(IEnumerable<KeyValuePair<string, string>> formData)
        {
            _formData = formData.ToList();
        }

        public RequestBodyFormUrlEncoded(IEnumerable<string> keys, IEnumerable<string> values)
        {
            var keyList = keys.ToList();
            var valueList = values.ToList();

            if (keyList.Count != valueList.Count)
            {
                throw new ArgumentException("Keys and values must have the same count");
            }

            _formData = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < keyList.Count; i++)
            {
                _formData.Add(new KeyValuePair<string, string>(keyList[i], valueList[i]));
            }
        }

        public IRequestBody Duplicate()
        {
            return new RequestBodyFormUrlEncoded(_formData);
        }

        public HttpContent ToHttpContent()
        {
            return new FormUrlEncodedContent(_formData);
        }

        public byte[] ToByteArray()
        {
            // Manually encode for byte array output
            var encoded = string.Join("&", _formData.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            return Encoding.UTF8.GetBytes(encoded);
        }

        public override string ToString()
        {
            return string.Join("&", _formData.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        }
    }
}
